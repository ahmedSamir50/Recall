using System.Diagnostics.CodeAnalysis;

namespace Recall.Core.Ocr;

public static class OcrModelFiles
{
    public const string Directory = "models/ocr";

    public const string GraphInputName = "x";

    public static IReadOnlyList<string> LocalNames =>
    [
        Detector.LocalName,
        EnglishReader.LocalName,
        ArabicReader.LocalName,
    ];

    public static class Detector
    {
        public const string Repo = "PaddlePaddle/PP-OCRv6_small_det_onnx";

        [SuppressMessage("Major Code Smell", "S1075:URIs should not be hardcoded", Justification = "The OCR download URLs are pinned model sources.")]
        public const string Url = "https://huggingface.co/PaddlePaddle/PP-OCRv6_small_det_onnx/resolve/main/";

        public const string RemoteName = "inference.onnx";

        public const string LocalName = "ppocrv6-small-det.onnx";

        public const string Sha256 = "d73e0058b7a8086bbd57f3d10b8bcd4ff95363f67e06e2762b5e814fe9c9410e";
    }

    public static class EnglishReader
    {
        public const string Repo = "PaddlePaddle/PP-OCRv6_small_rec_onnx";

        [SuppressMessage("Major Code Smell", "S1075:URIs should not be hardcoded", Justification = "The OCR download URLs are pinned model sources.")]
        public const string Url = "https://huggingface.co/PaddlePaddle/PP-OCRv6_small_rec_onnx/resolve/main/";

        public const string RemoteName = "inference.onnx";

        public const string LocalName = "ppocrv6-small-rec.onnx";

        public const string Sha256 = "5435fd747c9e0efe15a96d0b378d5bd157e9492ed8fd80edf08f30d02fa24634";
    }

    public static class EnglishReaderYaml
    {
        public const string RemoteName = "inference.yml";

        public const string Url = EnglishReader.Url;

        public const string LocalName = "en-inference.yml";

        public const string Sha256 = "ab078671bb49f06228eadccd34f1bb501e157f7a047095ffb943ba81512c77d1";
    }

    public static class ArabicReader
    {
        public const string Repo = "PaddlePaddle/arabic_PP-OCRv5_mobile_rec_onnx";

        [SuppressMessage("Major Code Smell", "S1075:URIs should not be hardcoded", Justification = "The OCR download URLs are pinned model sources.")]
        public const string Url = "https://huggingface.co/PaddlePaddle/arabic_PP-OCRv5_mobile_rec_onnx/resolve/main/";

        public const string RemoteName = "inference.onnx";

        public const string LocalName = "arabic-ppocrv5-mobile-rec.onnx";

        public const string Sha256 = "799113ebf267fbe742deb99eb36e8d42c9ddc5291ceacf92add41b4d52a59110";
    }

    public static class ArabicReaderYaml
    {
        public const string RemoteName = "inference.yml";

        public const string Url = ArabicReader.Url;

        public const string LocalName = "ar-inference.yml";

        public const string Sha256 = "21368419e6c016c31db55d316d59e11c128e1913e6e6fe10287084710043d3a6";
    }

    public static bool IsComplete(string appDataDirectory)
    {
        var directory = Path.Combine(appDataDirectory, Directory);
        return File.Exists(Path.Combine(directory, Detector.LocalName))
            && File.Exists(Path.Combine(directory, EnglishReader.LocalName))
            && File.Exists(Path.Combine(directory, EnglishReaderYaml.LocalName))
            && File.Exists(Path.Combine(directory, ArabicReader.LocalName))
            && File.Exists(Path.Combine(directory, ArabicReaderYaml.LocalName));
    }
}
