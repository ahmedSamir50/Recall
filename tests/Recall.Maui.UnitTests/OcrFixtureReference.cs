namespace Recall.Maui.UnitTests;

/// <summary>
/// On-page vocabulary for OCR proofs (invoice fields and book copy).
/// </summary>
internal static class OcrFixtureReference
{
    public const double InvoiceMaxMissingWordRate = 0.38;

    public const double BookPageMaxMissingWordRate = 0.25;

    public const string EnInvoice =
        "Fictional Gulf Foods Example Grocery Trading Dubai Invoice INV 2026 001 Date 2026 08 01 Currency AED " +
        "Sparkling Water 330ml SUP WAT 330 Quantity 24 EA Unit Price 2 50 Line Total 60 00 Subtotal 60 Tax 3 Total 63";

    public const string ArInvoice =
        "شركة الخليج الغذائية التجريبية شركة البقالة النموذجية دبي Invoice INV 2026 003 Date 2026 08 03 Currency AED " +
        "مياه غازية 330 مل SUP WAT 330 Quantity 24 Unit Price 2 50 Line Total 60 00 Subtotal 60 Tax 3 Total 63";

    public const string MixedInvoice =
        "Invoice INV 2026 004 Date 2026 08 04 Currency AED Sparkling Water 330ml SUP WAT 330 " +
        "Medjool Dates 1kg SUP DAT 1KG Quantity 24 8 Unit Price 2 50 4 50 Line Total 60 36 Subtotal 96 VAT 5 Total 100";

    public const string EnBookPage =
        "INTRODUCTION Charles Nicholl Nicholas Rowe William Shakespeare Some Account Life Mr Ben Jonson engraving Vandergucht Kneller";

    public const string ArBookPage =
        "فصوص الحكم جوهرة التصوف الإسلامي الفكر العرفاني الشيخ الأكبر ابن عربي محمد بن علي الحاتمي الطائي " +
        "دمشق التأليف بطاقة تعريفية بالكتاب والمؤلف قصة التأليف سر العنوان الرؤية المنامية";
}
