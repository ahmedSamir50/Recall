namespace Recall.Maui.UnitTests;

/// <summary>
/// Full visible text of each proof page. Invoice fields come from the SalorWorks
/// expected.json for that fixture, plus the banner and footer printed on the PNG.
/// Book pages are the full reading text of en_book_page.txt and ar_book_page.txt.
/// </summary>
internal readonly record struct OcrAcceptedWordError(
    int PageEdits,
    int PageTokens,
    int LatinEdits,
    int LatinTokens,
    int ArabicEdits,
    int ArabicTokens);

internal static class OcrFixtureReference
{
    /// <summary>
    /// Accepted on 26 Oct 2026 from tests/Recall.Maui.UnitTests/ocr-hypothesis.log.
    /// Picture words are embedded from this text. These edit ceilings are the fixture gate.
    /// A page fails when its edit count rises above the ceiling or its token count changes.
    /// </summary>
    public static readonly OcrAcceptedWordError EnInvoiceAccepted = new(35, 82, 35, 82, 0, 0);

    public static readonly OcrAcceptedWordError ArInvoiceAccepted = new(43, 89, 30, 61, 12, 28);

    public static readonly OcrAcceptedWordError MixedInvoiceAccepted = new(73, 116, 54, 94, 13, 22);

    public static readonly OcrAcceptedWordError EnBookAccepted = new(74, 162, 74, 162, 0, 0);

    public static readonly OcrAcceptedWordError ArBookAccepted = new(29, 153, 0, 0, 28, 153);

    public const string EnInvoice =
        "SYNTHETIC FIXTURE FICTIONAL SUPPLIER FOR SOFTWARE TESTING " +
        "Fictional Gulf Foods LLC " +
        "Warehouse 12 Al Quoz Dubai United Arab Emirates " +
        "TAX INVOICE " +
        "Invoice INV-2026-001 Date 2026-08-01 Currency AED " +
        "Bill to Example Grocery Trading LLC Dubai " +
        "SKU Description Qty Unit Price Total " +
        "SUP-WAT-330 Sparkling Water 330ml 24 EA 2.50 60.00 " +
        "Subtotal AED 60.00 VAT 5% AED 3.00 Total AED 63.00 " +
        "Fictional document Not a real supplier invoice Not tax customs or accounting advice";

    public const string ArInvoice =
        "SYNTHETIC FIXTURE FICTIONAL SUPPLIER FOR SOFTWARE TESTING " +
        "شركة الخليج الغذائية التجريبية ذ.م.م " +
        "المستودع 12 القوز دبي الإمارات العربية المتحدة " +
        "فاتورة ضريبية " +
        "Invoice INV-2026-003 Date 2026-08-03 Currency AED " +
        "Bill to شركة البقالة النموذجية ذ.م.م دبي " +
        "SKU Description Qty Unit Price Total " +
        "SUP-WAT-330 مياه غازية 330 مل 24 EA 2.50 60.00 " +
        "Subtotal AED 60.00 ضريبة القيمة المضافة 5% AED 3.00 Total AED 63.00 " +
        "Fictional document Not a real supplier invoice Not tax customs or accounting advice";

    public const string MixedInvoice =
        "SYNTHETIC FIXTURE FICTIONAL SUPPLIER FOR SOFTWARE TESTING " +
        "Fictional Gulf Foods LLC شركة الخليج الغذائية التجريبية ذ.م.م " +
        "Warehouse 12 Al Quoz Dubai المستودع 12 القوز دبي " +
        "TAX INVOICE فاتورة ضريبية " +
        "Invoice INV-2026-004 Date 2026-08-04 Currency AED " +
        "Bill to Example Grocery Trading LLC شركة البقالة النموذجية " +
        "SKU Description Qty Unit Price Total " +
        "SUP-WAT-330 Sparkling Water 330ml مياه غازية 330 مل 24 EA 2.50 60.00 " +
        "SUP-DAT-1KG Medjool Dates 1kg تمر مجهول 1 كجم 8 EA 4.50 36.00 " +
        "Subtotal AED 96.00 VAT 5% ضريبة 5% AED 4.80 Total AED 100.80 " +
        "Fictional document Not a real supplier invoice Not tax customs or accounting advice";

    public static string EnBookPage => ReadFixtureText("en_book_page.txt");

    public static string ArBookPage => ReadFixtureText("ar_book_page.txt");

    private static string ReadFixtureText(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "ocr-fixtures", fileName);
        return File.ReadAllText(path);
    }
}
