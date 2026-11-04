using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Unicode;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Recall.Core.Checkpoints;
using Recall.Core.Ocr;
using Recall.Core.Ports;
using Recall.Core.Runtime;
using Recall.Core.Text;

namespace Recall.Maui.Platform.Ocr;

public readonly record struct OcrLineRead(int EnglishCalls, int ArabicCalls, int Reads, double FirstConfidence, double Confidence, bool Arabic);

public sealed class OnnxOcrEngine : IOcrEngine
{
    public const double LowConfidence = 0.6;

    public const double ArabicLetterShare = 0.5;

    public const int OcrPageWorkers = 2;

    public const int MaxCpuThreads = 8;

    public static readonly TimeSpan OcrIdleUnload = TimeSpan.FromMinutes(2);

    private const string OutputCountMessage = "OCR graph output count is not 1.";

    private static readonly float[] DetectorMean = [0.485f, 0.456f, 0.406f];

    private static readonly float[] DetectorStd = [0.229f, 0.224f, 0.225f];

    private readonly string modelDirectory;
    private readonly IIngestCursorStore cursors;
    private readonly IMediaVectorIndex mediaIndex;
    private readonly TextIngestEngine textIngest;
    private readonly IMemoryBudget memory;
    private readonly OcrSessionSet sessions = new();
    private readonly Dictionary<string, string> recognizedByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<OcrLineRead> lineReads = [];
    private readonly object lineReadGate = new();

    private IReadOnlyList<string> englishDict = [];
    private IReadOnlyList<string> arabicDict = [];
    private long lastJobTicks = DateTimeOffset.UtcNow.UtcTicks;
    private int activeJobs;
    private Timer? idleTimer;

    public OnnxOcrEngine(
        string modelDirectory,
        IIngestCursorStore cursors,
        IMediaVectorIndex mediaIndex,
        TextIngestEngine textIngest,
        IMemoryBudget memory)
    {
        this.modelDirectory = modelDirectory;
        this.cursors = cursors;
        this.mediaIndex = mediaIndex;
        this.textIngest = textIngest;
        this.memory = memory;
    }

    public OcrExecutionProvider Provider { get; private set; } = OcrExecutionProvider.Cpu;

    public bool DetectorLoaded => sessions.Detector is not null;

    public bool EnglishReaderLoaded => sessions.EnglishReader is not null;

    public bool ArabicReaderLoaded => sessions.ArabicReader is not null;

    public OcrSessionSet Sessions => sessions;

    public IReadOnlyDictionary<string, string> RecognizedByPath => recognizedByPath;

    public IReadOnlyList<OcrLineRead> LineReads
    {
        get
        {
            lock (lineReadGate)
            {
                return [.. lineReads];
            }
        }
    }

    public int PageWorkers
    {
        get
        {
            var text = Environment.GetEnvironmentVariable("RECALL_OCR_PAGE_WORKERS");
            var workers = int.TryParse(text, out var parsed) && parsed > 0 ? parsed : OcrPageWorkers;
            return memory.Pressure == MemoryPressure.Hard ? 1 : workers;
        }
    }

    public static int ThreadCountFor(OcrExecutionProvider provider, int cores)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cores);
        return provider == OcrExecutionProvider.DirectMl ? 1 : Math.Clamp(cores - 1, 1, MaxCpuThreads);
    }

    public static List<PdfPageText> JoinInPageOrder(IReadOnlyDictionary<int, string> pageTexts)
    {
        ArgumentNullException.ThrowIfNull(pageTexts);
        var ordered = new List<PdfPageText>(pageTexts.Count);
        foreach (var pageNumber in pageTexts.Keys.Order())
        {
            ordered.Add(new PdfPageText(pageNumber, pageTexts[pageNumber]));
        }

        return ordered;
    }

    public async Task RunPassAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paths);
        recognizedByPath.Clear();
        lock (lineReadGate)
        {
            lineReads.Clear();
        }

        var pending = new List<string>();
        foreach (var path in paths)
        {
            if (ShouldSkipPath(path))
            {
                continue;
            }

            pending.Add(path);
        }

        if (pending.Count == 0)
        {
            return;
        }

        Interlocked.Increment(ref activeJobs);
        Interlocked.Exchange(ref lastJobTicks, DateTimeOffset.UtcNow.UtcTicks);
        try
        {
            OcrRunProgress.BeginPass(pending.Count);
            EnsureLoaded();
            StartIdleTimer();
            for (var index = 0; index < pending.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = pending[index];
                OcrRunProgress.BeginPath(index + 1, pending.Count, path);
                if (path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    var pageTexts = await RecognizePdfAsync(path, cancellationToken).ConfigureAwait(false);
                    if (pageTexts.Count == 0)
                    {
                        OcrRunProgress.EndPath(index + 1, pending.Count, path, 0);
                        continue;
                    }

                    var joined = JoinPdfText(pageTexts);
                    recognizedByPath[path] = joined;
                    PersistPdfPages(path, pageTexts);
                    OcrRunProgress.EndPath(index + 1, pending.Count, path, joined.Length);
                    continue;
                }

                var text = RecognizeImagePath(path);
                if (string.IsNullOrEmpty(text))
                {
                    OcrRunProgress.EndPath(index + 1, pending.Count, path, 0);
                    continue;
                }

                recognizedByPath[path] = text;
                PersistImageWords(path, text);
                OcrRunProgress.EndPath(index + 1, pending.Count, path, text.Length);
            }

            OcrRunProgress.EndPass(recognizedByPath.Count);
        }
        finally
        {
            Interlocked.Exchange(ref lastJobTicks, DateTimeOffset.UtcNow.UtcTicks);
            Interlocked.Decrement(ref activeJobs);
        }
    }

    public bool UnloadIfIdle(DateTimeOffset now)
    {
        if (Volatile.Read(ref activeJobs) > 0)
        {
            return false;
        }

        if (!sessions.Loaded)
        {
            return false;
        }

        var lastJob = new DateTimeOffset(Interlocked.Read(ref lastJobTicks), TimeSpan.Zero);
        if (now - lastJob < OcrIdleUnload)
        {
            return false;
        }

        UnloadInferenceSessions();
        return true;
    }

    public void Dispose()
    {
        idleTimer?.Dispose();
        idleTimer = null;
        UnloadInferenceSessions();
    }

    private static string JoinPdfText(IReadOnlyList<PdfPageText> pages)
    {
        var builder = new StringBuilder();
        for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            if (pageIndex > 0)
            {
                builder.Append(' ');
            }

            builder.Append(pages[pageIndex].Text);
        }

        return builder.ToString();
    }

    private static OcrExecutionProvider PreferredProvider()
    {
        var text = Environment.GetEnvironmentVariable("RECALL_OCR_EXECUTION_PROVIDER");
        if (string.Equals(text, "cpu", StringComparison.OrdinalIgnoreCase))
        {
            return OcrExecutionProvider.Cpu;
        }

        return OperatingSystem.IsWindows() ? OcrExecutionProvider.DirectMl : OcrExecutionProvider.Cpu;
    }

    private static int IntraOpThreads(OcrExecutionProvider provider)
    {
        var threadText = Environment.GetEnvironmentVariable("RECALL_OCR_INTRA_OP_THREADS");
        if (int.TryParse(threadText, out var threads) && threads > 0)
        {
            return threads;
        }

        return ThreadCountFor(provider, Environment.ProcessorCount);
    }

    private static bool IsProviderUnavailable(Exception exception) =>
        exception is OnnxRuntimeException or DllNotFoundException or EntryPointNotFoundException;

    private static bool ScriptOf(string text)
    {
        var arabic = 0;
        var latin = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (IsArabicLetter(rune))
            {
                arabic++;
            }
            else if (IsLatinLetter(rune))
            {
                latin++;
            }
        }

        var letters = arabic + latin;
        return letters > 0 && arabic / (double)letters > ArabicLetterShare;
    }

    private static bool IsArabicLetter(Rune rune) =>
        rune.Value >= UnicodeRanges.Arabic.FirstCodePoint
        && rune.Value < UnicodeRanges.Arabic.FirstCodePoint + UnicodeRanges.Arabic.Length;

    private static bool IsLatinLetter(Rune rune) =>
        (rune.Value >= 'A' && rune.Value <= 'Z')
        || (rune.Value >= 'a' && rune.Value <= 'z')
        || (rune.Value >= 0x00C0 && rune.Value <= 0x024F);

    private void StartIdleTimer()
    {
        idleTimer ??= new Timer(_ => UnloadIfIdle(DateTimeOffset.UtcNow), null, OcrIdleUnload, OcrIdleUnload);
    }

    private void UnloadInferenceSessions()
    {
        sessions.Dispose();
        englishDict = [];
        arabicDict = [];
    }

    private bool ShouldSkipPath(string path)
    {
        if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var pages = cursors.ListOcrPages(path);
        return pages.Count == 0;
    }

    private string RecognizeImagePath(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        var bytes = File.ReadAllBytes(path);
        return RecognizeImage(bytes, out _, out _);
    }

    private async Task<List<PdfPageText>> RecognizePdfAsync(string path, CancellationToken cancellationToken)
    {
        var pages = cursors.ListOcrPages(path);
        var pageTexts = new ConcurrentDictionary<int, string>();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = PageWorkers,
            CancellationToken = cancellationToken,
        };
#if WINDOWS
        using var document = WindowsPdfDocument.Open(path);
#endif
        await Parallel.ForEachAsync(pages, options, (pageNumber, token) =>
        {
            token.ThrowIfCancellationRequested();
            var renderWatch = Stopwatch.StartNew();
#if WINDOWS
            var png = document.Render(pageNumber);
#else
            var png = RenderPdfPage(path, pageNumber);
#endif
            renderWatch.Stop();
            var inferWatch = Stopwatch.StartNew();
            var pageText = RecognizeImage(png, out var detectorMs, out var readerMs);
            inferWatch.Stop();
            OcrRunProgress.Message(
                $"ocr page {pageNumber} render={renderWatch.ElapsedMilliseconds}ms detector={detectorMs}ms reader={readerMs}ms infer={inferWatch.ElapsedMilliseconds}ms");
            if (!string.IsNullOrEmpty(pageText))
            {
                pageTexts[pageNumber] = pageText;
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
        return JoinInPageOrder(pageTexts);
    }

    private void PersistImageWords(string path, string text)
    {
        IngestChunkRow? matched = null;
        var matches = 0;
        foreach (var row in cursors.ListChunks(path))
        {
            if (row.Retired || row.Missing || row.Ordinal != 0)
            {
                continue;
            }

            matched = row;
            matches++;
        }

        if (matches == 1)
        {
            mediaIndex.TrySetWords(matched!.ContentId, text);
        }
    }

    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "OCR ingest runs on the ingest Task.Run loop, not the UI thread.")]
    private void PersistPdfPages(string path, IReadOnlyList<PdfPageText> pageTexts)
    {
        var row = cursors.GetFile(path);
        if (pageTexts.Count == 0 || row is null)
        {
            return;
        }

        textIngest.IngestOcrPagesAsync(path, pageTexts, row.GrantPath, row.EncoderHash, CancellationToken.None).GetAwaiter().GetResult();
    }

    private string RecognizeImage(byte[] imageBytes, out long detectorMs, out long readerMs)
    {
        var raster = OcrRasterDecoder.Decode(imageBytes);
        var detectorWatch = Stopwatch.StartNew();
        var boxes = DetectBoxes(raster);
        detectorWatch.Stop();
        detectorMs = detectorWatch.ElapsedMilliseconds;
        readerMs = 0;
        if (boxes.Count == 0)
        {
            return string.Empty;
        }

        var readerWatch = Stopwatch.StartNew();
        var texts = new List<string>(boxes.Count);
        foreach (var box in boxes)
        {
            texts.Add(ReadLine(Crop(raster, box)));
        }

        readerWatch.Stop();
        readerMs = readerWatch.ElapsedMilliseconds;
        return JoinLines(texts);
    }

    private static string JoinLines(IReadOnlyList<string> texts)
    {
        var builder = new StringBuilder();
        foreach (var text in texts)
        {
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (builder.Length > 0 && !char.IsWhiteSpace(builder[^1]))
            {
                builder.Append(' ');
            }

            builder.Append(text);
        }

        return builder.ToString();
    }

    private string ReadLine(OcrRaster crop)
    {
        var (english, englishScore) = ReadCrop(crop, sessions.EnglishReader!, englishDict, visualArabic: false);
        var (arabic, arabicScore) = ReadCrop(crop, sessions.ArabicReader!, arabicDict, visualArabic: true);
        var chosen = OcrLineChoice.Union(english, englishScore, arabic, arabicScore);
        var score = Math.Max(englishScore, arabicScore);
        var arabicLine = chosen.Length > 0 ? ScriptOf(chosen) : arabicScore > englishScore;
        RecordLineRead(englishCalls: 1, arabicCalls: 1, reads: 2, firstConfidence: score, confidence: score, arabic: arabicLine);
        return chosen;
    }

    private void RecordLineRead(int englishCalls, int arabicCalls, int reads, double firstConfidence, double confidence, bool arabic)
    {
        lock (lineReadGate)
        {
            lineReads.Add(new OcrLineRead(englishCalls, arabicCalls, reads, firstConfidence, confidence, arabic));
        }
    }

    private IReadOnlyList<OcrBox> DetectBoxes(OcrRaster raster)
    {
        var (tensor, mapWidth, mapHeight, scaleX, scaleY) = BuildDetectorInput(raster);
        using var results = sessions.Detector!.Run([NamedOnnxValue.CreateFromTensor(OcrModelFiles.GraphInputName, tensor)]);
        if (results.Count != 1)
        {
            throw new InvalidOperationException(OutputCountMessage);
        }

        var output = results[0].AsTensor<float>();
        var map = output.ToArray();
        var boxes = DbBoxExtractor.Extract(map, mapWidth, mapHeight);
        var scaled = new List<OcrBox>();
        foreach (var box in boxes)
        {
            var x = (int)Math.Round(box.X / scaleX);
            var y = (int)Math.Round(box.Y / scaleY);
            var w = (int)Math.Round(box.Width / scaleX);
            var h = (int)Math.Round(box.Height / scaleY);
            scaled.Add(new OcrBox(x, y, w, h, box.Score));
        }

        return scaled;
    }

    [SuppressMessage("Major Code Smell", "S4143:Collection elements should not be overwritten", Justification = "Each tensor channel index is distinct.")]
    private static (DenseTensor<float> Tensor, int MapWidth, int MapHeight, double ScaleX, double ScaleY) BuildDetectorInput(OcrRaster raster)
    {
        var (resizedWidth, resizedHeight) = OcrDetectorResize.Size(raster.Width, raster.Height);
        var scaleX = resizedWidth / (double)raster.Width;
        var scaleY = resizedHeight / (double)raster.Height;
        var tensor = new DenseTensor<float>([1, 3, resizedHeight, resizedWidth]);
        for (var y = 0; y < resizedHeight; y++)
        {
            for (var x = 0; x < resizedWidth; x++)
            {
                var sample = OcrBilinear.Sample(raster.Bgra, raster.Width, raster.Height, x, y, resizedWidth, resizedHeight);
                tensor[0, 0, y, x] = (sample.B / 255f - DetectorMean[0]) / DetectorStd[0];
                tensor[0, 1, y, x] = (sample.G / 255f - DetectorMean[1]) / DetectorStd[1];
                tensor[0, 2, y, x] = (sample.R / 255f - DetectorMean[2]) / DetectorStd[2];
            }
        }

        return (tensor, resizedWidth, resizedHeight, scaleX, scaleY);
    }

    private static OcrRaster Crop(OcrRaster source, OcrBox box)
    {
        var x0 = Math.Clamp(box.X, 0, source.Width - 1);
        var y0 = Math.Clamp(box.Y, 0, source.Height - 1);
        var x1 = Math.Clamp(box.X + box.Width, 1, source.Width);
        var y1 = Math.Clamp(box.Y + box.Height, 1, source.Height);
        var width = x1 - x0;
        var height = y1 - y0;
        var bgra = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 4;
                var srcOffset = ((y0 + y) * source.Width + (x0 + x)) * 4;
                Array.Copy(source.Bgra, srcOffset, bgra, offset, 4);
            }
        }

        return new OcrRaster(width, height, bgra);
    }

    private static (string Text, double Confidence) ReadCrop(OcrRaster crop, InferenceSession reader, IReadOnlyList<string> dict, bool visualArabic)
    {
        var tensor = BuildReaderInput(crop);
        using var results = reader.Run([NamedOnnxValue.CreateFromTensor(OcrModelFiles.GraphInputName, tensor)]);
        if (results.Count != 1)
        {
            throw new InvalidOperationException(OutputCountMessage);
        }

        var output = results[0].AsTensor<float>();
        var dims = output.Dimensions.ToArray();
        var timeSteps = dims.Length >= 2 ? dims[1] : 1;
        var classes = dims.Length >= 3 ? dims[2] : dict.Count + 1;
        return CtcDecoder.Decode(output.ToArray(), timeSteps, classes, dict, visualArabic);
    }

    [SuppressMessage("Major Code Smell", "S4143:Collection elements should not be overwritten", Justification = "Each tensor channel index is distinct.")]
    private static DenseTensor<float> BuildReaderInput(OcrRaster crop)
    {
        var canvas = OcrReaderResize.Canvas(crop.Width, crop.Height);
        var tensor = new DenseTensor<float>([1, 3, OcrReaderResize.Height, canvas.CanvasWidth]);
        for (var y = 0; y < OcrReaderResize.Height; y++)
        {
            for (var x = 0; x < canvas.CanvasWidth; x++)
            {
                if (x < canvas.ContentWidth)
                {
                    var sample = OcrBilinear.Sample(crop.Bgra, crop.Width, crop.Height, x, y, canvas.ContentWidth, OcrReaderResize.Height);
                    tensor[0, 0, y, x] = (sample.B / 255f - 0.5f) / 0.5f;
                    tensor[0, 1, y, x] = (sample.G / 255f - 0.5f) / 0.5f;
                    tensor[0, 2, y, x] = (sample.R / 255f - 0.5f) / 0.5f;
                }
                else
                {
                    tensor[0, 0, y, x] = 0f;
                    tensor[0, 1, y, x] = 0f;
                    tensor[0, 2, y, x] = 0f;
                }
            }
        }

        return tensor;
    }

#if !WINDOWS
    private static byte[] RenderPdfPage(string path, int pageNumber)
    {
#if ANDROID
        return AndroidPdfPageRenderer.Render(path, pageNumber);
#elif IOS || MACCATALYST
        return ApplePdfPageRenderer.Render(path, pageNumber);
#else
        throw new PlatformNotSupportedException();
#endif
    }
#endif

    private void EnsureLoaded()
    {
        if (sessions.Loaded)
        {
            return;
        }

        var directory = Path.Combine(modelDirectory, OcrModelFiles.Directory);
        CreateSession(directory);
        englishDict = OcrInferenceYaml.ReadCharacterDict(Path.Combine(directory, OcrModelFiles.EnglishReaderYaml.LocalName));
        arabicDict = OcrInferenceYaml.ReadCharacterDict(Path.Combine(directory, OcrModelFiles.ArabicReaderYaml.LocalName));
    }

    private void CreateSession(string directory)
    {
        var provider = PreferredProvider();
        var threads = IntraOpThreads(provider);
        try
        {
            sessions.Load(directory, provider, threads);
            Provider = provider;
        }
        catch (Exception exception) when (provider == OcrExecutionProvider.DirectMl && IsProviderUnavailable(exception))
        {
            OcrRunProgress.Message($"ocr provider fallback: {exception.GetType().Name}: {exception.Message}");
            sessions.Dispose();
            sessions.Load(directory, OcrExecutionProvider.Cpu, IntraOpThreads(OcrExecutionProvider.Cpu));
            Provider = OcrExecutionProvider.Cpu;
        }

        OcrRunProgress.Message($"ocr sessions provider={Provider} intraOp={IntraOpThreads(Provider)} pageWorkers={PageWorkers}");
    }
}
