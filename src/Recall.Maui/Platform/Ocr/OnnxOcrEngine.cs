using System.Diagnostics.CodeAnalysis;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Recall.Core.Checkpoints;
using Recall.Core.Ocr;
using Recall.Core.Ports;
using Recall.Core.Text;

namespace Recall.Maui.Platform.Ocr;

public sealed class OnnxOcrEngine : IOcrEngine
{
    private const string OutputCountMessage = "OCR graph output count is not 1.";

    private const int DetectorLongSide = 736;

    private const int ReaderHeight = 48;

    private const int ReaderWidth = 320;

    private static readonly float[] DetectorMean = [0.485f, 0.456f, 0.406f];

    private static readonly float[] DetectorStd = [0.229f, 0.224f, 0.225f];

    private readonly string modelDirectory;
    private readonly IIngestCursorStore cursors;
    private readonly IMediaVectorIndex mediaIndex;
    private readonly TextIngestEngine textIngest;
    private readonly OcrSessionSet sessions = new();
    private readonly Dictionary<string, string> recognizedByPath = new(StringComparer.OrdinalIgnoreCase);

    private InferenceSession? detector;
    private InferenceSession? englishReader;
    private InferenceSession? arabicReader;
    private IReadOnlyList<string> englishDict = [];
    private IReadOnlyList<string> arabicDict = [];
    private bool loaded;

    public OnnxOcrEngine(string modelDirectory, IIngestCursorStore cursors, IMediaVectorIndex mediaIndex, TextIngestEngine textIngest)
    {
        this.modelDirectory = modelDirectory;
        this.cursors = cursors;
        this.mediaIndex = mediaIndex;
        this.textIngest = textIngest;
    }

    public bool DetectorLoaded => sessions.DetectorLoaded;

    public IReadOnlyDictionary<string, string> RecognizedByPath => recognizedByPath;

    public void RunPass(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        recognizedByPath.Clear();
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

        EnsureLoaded();
        foreach (var path in pending)
        {
            if (path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var pageTexts = RecognizePdf(path);
                if (pageTexts.Count == 0)
                {
                    continue;
                }

                recognizedByPath[path] = JoinPdfText(pageTexts);
                PersistPdfPages(path, pageTexts);
                continue;
            }

            var text = RecognizeImagePath(path);
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            recognizedByPath[path] = text;
            PersistImageWords(path, text);
        }
    }

    public void Dispose()
    {
        detector?.Dispose();
        englishReader?.Dispose();
        arabicReader?.Dispose();
        detector = null;
        englishReader = null;
        arabicReader = null;
        loaded = false;
        sessions.Dispose();
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

    private static string JoinPdfText(IReadOnlyList<PdfPageText> pages)
    {
        var builder = new System.Text.StringBuilder();
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

    private string RecognizeImagePath(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        var bytes = File.ReadAllBytes(path);
        return RecognizeImage(bytes);
    }

    private List<PdfPageText> RecognizePdf(string path)
    {
        var pages = cursors.ListOcrPages(path);
        var result = new List<PdfPageText>();
        foreach (var pageNumber in pages)
        {
            var png = RenderPdfPage(path, pageNumber);
            var pageText = RecognizeImage(png);
            if (!string.IsNullOrEmpty(pageText))
            {
                result.Add(new PdfPageText(pageNumber, pageText));
            }
        }

        return result;
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

    private string RecognizeImage(byte[] imageBytes)
    {
        var raster = OcrRasterDecoder.Decode(imageBytes);
        var boxes = DetectBoxes(raster);
        if (boxes.Count == 0)
        {
            return string.Empty;
        }

        var lines = new List<OcrLinePair>();
        foreach (var box in boxes)
        {
            var crop = Crop(raster, box);
            var (english, englishScore) = ReadCrop(crop, englishReader!, englishDict);
            var (arabic, arabicScore) = ReadCrop(crop, arabicReader!, arabicDict);
            var script = OcrPageRouter.ContainsArabicScript(arabic) || OcrPageRouter.ContainsArabicScript(english);
            lines.Add(new OcrLinePair(english, englishScore, arabic, arabicScore, script));
        }

        return OcrPageRouter.Route(lines);
    }

    private IReadOnlyList<OcrBox> DetectBoxes(OcrRaster raster)
    {
        var (tensor, mapWidth, mapHeight, scaleX, scaleY) = BuildDetectorInput(raster);
        using var results = detector!.Run([NamedOnnxValue.CreateFromTensor(OcrModelFiles.GraphInputName, tensor)]);
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
        var longSide = Math.Max(raster.Width, raster.Height);
        var scale = DetectorLongSide / (double)longSide;
        var resizedWidth = AlignDetectorSide((int)Math.Round(raster.Width * scale));
        var resizedHeight = AlignDetectorSide((int)Math.Round(raster.Height * scale));
        var scaleX = resizedWidth / (double)raster.Width;
        var scaleY = resizedHeight / (double)raster.Height;
        var tensor = new DenseTensor<float>([1, 3, resizedHeight, resizedWidth]);
        for (var y = 0; y < resizedHeight; y++)
        {
            for (var x = 0; x < resizedWidth; x++)
            {
                var srcX = Math.Clamp((int)Math.Round(x / scaleX), 0, raster.Width - 1);
                var srcY = Math.Clamp((int)Math.Round(y / scaleY), 0, raster.Height - 1);
                var b = raster.GetB(srcX, srcY) / 255f;
                var g = raster.GetG(srcX, srcY) / 255f;
                var r = raster.GetR(srcX, srcY) / 255f;
                tensor[0, 0, y, x] = (b - DetectorMean[0]) / DetectorStd[0];
                tensor[0, 1, y, x] = (g - DetectorMean[1]) / DetectorStd[1];
                tensor[0, 2, y, x] = (r - DetectorMean[2]) / DetectorStd[2];
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

    private static (string Text, double Confidence) ReadCrop(OcrRaster crop, InferenceSession reader, IReadOnlyList<string> dict)
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
        return CtcDecoder.Decode(output.ToArray(), timeSteps, classes, dict);
    }

    [SuppressMessage("Major Code Smell", "S4143:Collection elements should not be overwritten", Justification = "Each tensor channel index is distinct.")]
    private static DenseTensor<float> BuildReaderInput(OcrRaster crop)
    {
        var tensor = new DenseTensor<float>([1, 3, ReaderHeight, ReaderWidth]);
        for (var y = 0; y < ReaderHeight; y++)
        {
            for (var x = 0; x < ReaderWidth; x++)
            {
                var srcX = crop.Width <= 1 ? 0 : x * (crop.Width - 1) / (ReaderWidth - 1);
                var srcY = crop.Height <= 1 ? 0 : y * (crop.Height - 1) / (ReaderHeight - 1);
                var b = crop.GetB(srcX, srcY) / 255f;
                var g = crop.GetG(srcX, srcY) / 255f;
                var r = crop.GetR(srcX, srcY) / 255f;
                tensor[0, 0, y, x] = (b - 0.5f) / 0.5f;
                tensor[0, 1, y, x] = (g - 0.5f) / 0.5f;
                tensor[0, 2, y, x] = (r - 0.5f) / 0.5f;
            }
        }

        return tensor;
    }

    private static byte[] RenderPdfPage(string path, int pageNumber)
    {
#if WINDOWS
        return WindowsPdfPageRenderer.Render(path, pageNumber);
#elif ANDROID
        return AndroidPdfPageRenderer.Render(path, pageNumber);
#elif IOS || MACCATALYST
        return ApplePdfPageRenderer.Render(path, pageNumber);
#else
        throw new PlatformNotSupportedException();
#endif
    }

    private void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        var directory = Path.Combine(modelDirectory, OcrModelFiles.Directory);
        detector = CreateSession(Path.Combine(directory, OcrModelFiles.Detector.LocalName));
        englishReader = CreateSession(Path.Combine(directory, OcrModelFiles.EnglishReader.LocalName));
        arabicReader = CreateSession(Path.Combine(directory, OcrModelFiles.ArabicReader.LocalName));
        englishDict = OcrInferenceYaml.ReadCharacterDict(Path.Combine(directory, OcrModelFiles.EnglishReaderYaml.LocalName));
        arabicDict = OcrInferenceYaml.ReadCharacterDict(Path.Combine(directory, OcrModelFiles.ArabicReaderYaml.LocalName));

        sessions.Load();
        loaded = true;
    }

    private static int AlignDetectorSide(int side) => Math.Max(32, (int)Math.Ceiling(side / 32.0) * 32);

    private static InferenceSession CreateSession(string modelPath)
    {
        var options = new SessionOptions
        {
            EnableMemoryPattern = false,
            EnableCpuMemArena = false,
        };
        return new InferenceSession(modelPath, options);
    }
}
