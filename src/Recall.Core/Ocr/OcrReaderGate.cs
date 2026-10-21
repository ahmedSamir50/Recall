namespace Recall.Core.Ocr;

public static class OcrReaderGate
{
    public static bool CanLoad(string? sha256)
    {
        if (string.IsNullOrWhiteSpace(sha256))
        {
            return false;
        }

        return string.Equals(sha256, OcrModelFiles.Detector.Sha256, StringComparison.Ordinal)
            || string.Equals(sha256, OcrModelFiles.EnglishReader.Sha256, StringComparison.Ordinal)
            || string.Equals(sha256, OcrModelFiles.ArabicReader.Sha256, StringComparison.Ordinal);
    }
}
