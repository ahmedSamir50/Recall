#if WINDOWS
using Recall.Core.Ocr;
using Recall.IntegrationTests.Infrastructure;
using Recall.Maui.Platform.Ocr;

namespace Recall.IntegrationTests;

internal readonly record struct OcrGoldenPage(byte[] Png, string GroundTruth, bool IsPhone);

internal static class OcrGoldenCatalog
{
    // Synthetic note slices: half A4 keeps CER stable while 80-page golden finishes in minutes, not hours.
    private const int GoldenTextWidth = 1240;
    private const int GoldenTextHeight = 1754;

    public static IReadOnlyList<OcrGoldenPage> Build()
    {
        DemoDatasetPaths.RequireRoot();
        var pages = new List<OcrGoldenPage>();
        AddRenderedSlices(
            pages,
            DemoDatasetPaths.Combine("notes", "ar_long_حي_بن_يقظان_ابن_طفيل.txt"),
            29,
            isPhone: false);
        pages.Add(RenderImage(
            DemoDatasetPaths.Combine("images", "sign_ar_shop_alexandria.jpg"),
            DemoDatasetPaths.Combine("ground_truth", "images", "sign_ar_shop_alexandria.gt.txt"),
            isPhone: true));

        AddRenderedSlices(
            pages,
            DemoDatasetPaths.Combine("notes", "en_long_jekyll_and_hyde_stevenson.txt"),
            28,
            isPhone: false);
        pages.Add(RenderPdfPage(
            DemoDatasetPaths.Combine("pdf_scanned", "en_phone_blood_test_report.pdf"),
            DemoDatasetPaths.Combine("ground_truth", "pdf_scanned", "en_phone_blood_test_report.gt.txt"),
            isPhone: true));
        pages.Add(RenderImage(
            DemoDatasetPaths.Combine("images", "receipt_en_supermarket.png"),
            DemoDatasetPaths.Combine("ground_truth", "images", "receipt_en_supermarket.gt.txt"),
            isPhone: true));

        var european = string.Concat(
            File.ReadAllText(DemoDatasetPaths.Combine("notes", "fr_recette_ratatouille.md")),
            File.ReadAllText(DemoDatasetPaths.Combine("notes", "de_fahrrad_reparatur.md")),
            File.ReadAllText(DemoDatasetPaths.Combine("notes", "es_viaje_granada.md")));
        AddRenderedSlicesFromText(pages, european, 18, isPhone: false);
        pages.Add(RenderPdfPage(
            DemoDatasetPaths.Combine("pdf_scanned", "de_clean_fitnessstudio_kuendigung.pdf"),
            DemoDatasetPaths.Combine("ground_truth", "pdf_scanned", "de_clean_fitnessstudio_kuendigung.gt.txt"),
            isPhone: false));
        pages.Add(RenderImage(
            DemoDatasetPaths.Combine("images", "plaque_fr_rue_de_richelieu.jpg"),
            DemoDatasetPaths.Combine("ground_truth", "images", "plaque_fr_rue_de_richelieu.gt.txt"),
            isPhone: true));

        return pages;
    }

    private static void AddRenderedSlices(List<OcrGoldenPage> pages, string textPath, int count, bool isPhone)
    {
        var text = File.ReadAllText(textPath);
        AddRenderedSlicesFromText(pages, text, count, isPhone);
    }

    private static void AddRenderedSlicesFromText(List<OcrGoldenPage> pages, string text, int count, bool isPhone)
    {
        foreach (var slice in OcrGoldenPages.SplitEqual(text, count))
        {
            pages.Add(new OcrGoldenPage(WindowsTextPageRenderer.Render(slice, GoldenTextWidth, GoldenTextHeight), slice, isPhone));
        }
    }

    private static OcrGoldenPage RenderPdfPage(string pdfPath, string gtPath, bool isPhone)
    {
        if (!File.Exists(pdfPath) || !File.Exists(gtPath))
        {
            throw new FileNotFoundException($"Golden PDF pair missing: {pdfPath}");
        }
        var png = WindowsPdfPageRenderer.Render(pdfPath, 1);
        return new OcrGoldenPage(png, File.ReadAllText(gtPath).Trim(), isPhone);
    }

    private static OcrGoldenPage RenderImage(string imagePath, string gtPath, bool isPhone)
    {
        if (!File.Exists(imagePath) || !File.Exists(gtPath))
        {
            throw new FileNotFoundException($"Golden image pair missing: {imagePath}");
        }

        return new OcrGoldenPage(File.ReadAllBytes(imagePath), File.ReadAllText(gtPath).Trim(), isPhone);
    }
}
#endif
