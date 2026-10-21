namespace Recall.Core.Ocr;

public sealed class OcrSessionSet : IDisposable
{
    public bool DetectorLoaded { get; private set; }

    public bool EnglishReaderLoaded { get; private set; }

    public bool ArabicReaderLoaded { get; private set; }

    public void Load()
    {
        DetectorLoaded = true;
        EnglishReaderLoaded = true;
        ArabicReaderLoaded = true;
    }

    public void Dispose()
    {
        DetectorLoaded = false;
        EnglishReaderLoaded = false;
        ArabicReaderLoaded = false;
    }
}
