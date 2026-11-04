using Microsoft.ML.OnnxRuntime;
using Recall.Core.Ocr;

namespace Recall.Maui.Platform.Ocr;

public sealed class OcrSessionSet : IDisposable
{
    public InferenceSession? Detector { get; private set; }

    public InferenceSession? EnglishReader { get; private set; }

    public InferenceSession? ArabicReader { get; private set; }

    public bool Loaded => Detector is not null && EnglishReader is not null && ArabicReader is not null;

    public void Load(string directory, OcrExecutionProvider provider, int threads)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threads);
        Dispose();
        try
        {
            var options = CreateOptions(provider, threads);
            Detector = new InferenceSession(Path.Combine(directory, OcrModelFiles.Detector.LocalName), options);
            EnglishReader = new InferenceSession(Path.Combine(directory, OcrModelFiles.EnglishReader.LocalName), options);
            ArabicReader = new InferenceSession(Path.Combine(directory, OcrModelFiles.ArabicReader.LocalName), options);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        Detector?.Dispose();
        EnglishReader?.Dispose();
        ArabicReader?.Dispose();
        Detector = null;
        EnglishReader = null;
        ArabicReader = null;
    }

    private static SessionOptions CreateOptions(OcrExecutionProvider provider, int threads)
    {
        var options = new SessionOptions
        {
            EnableMemoryPattern = false,
            EnableCpuMemArena = false,
            IntraOpNumThreads = threads,
            InterOpNumThreads = 1,
        };

        if (provider == OcrExecutionProvider.DirectMl && OperatingSystem.IsWindows())
        {
            options.AppendExecutionProvider_DML(0);
        }

        return options;
    }
}
