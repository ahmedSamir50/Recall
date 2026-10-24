using Recall.Core.Ocr;

namespace Recall.Core.Text;

public readonly record struct PdfPageText(int PageNumber, string Text);

public readonly record struct PdfPatch(string Raw, int Ordinal, int PageStart, int PageEnd, int CharStart, int CharEnd);

public readonly record struct PdfPageSplit(
    string JoinedDocument,
    IReadOnlyList<PdfPageText> EmbeddablePages,
    IReadOnlyList<int> OcrPageNumbers);

public static class DocumentPatchPlanner
{
    public const int PatchTargetTokens = 2048;

    public const int LookbackTokens = 512;

    public const int OverlapTokens = 256;

    public const int MaxOverlapTokens = 384;

    public const int HardCapTokens = 7680;

    public static bool EndsPatch(int prefixedTokenCount) => prefixedTokenCount >= HardCapTokens;

    public static int OpenPage(int? page) => page is null || page < 1 ? 1 : page.Value;

    private static readonly string[] Abbreviations = ["e.g.", "i.e.", "etc.", "vs.", "dr.", "mr.", "no."];

    private const int ParagraphClass = 10;

    private const int PageBreakClass = 9;

    private const int HeadingClass = 8;

    private const int SentenceClass = 7;

    private const int LineBreakClass = 6;

    private const int ArabicSemicolonClass = 5;

    private const int ArabicCommaClass = 4;

    private const int WhitespaceClass = 1;

    private const char ArabicSemicolon = '؛';

    private const char ArabicComma = '،';

    public static bool IsOcrPage(string pageText)
    {
        ArgumentNullException.ThrowIfNull(pageText);
        if (pageText.IndexOf('\uFFFD') < 0)
        {
            return false;
        }

        var start = 0;
        while (start < pageText.Length)
        {
            while (start < pageText.Length && IsWordSeparator(pageText[start]))
            {
                start++;
            }

            if (start >= pageText.Length)
            {
                return false;
            }

            var end = start;
            while (end < pageText.Length && !IsWordSeparator(pageText[end]))
            {
                end++;
            }

            if (BrokenArabic.NeedsOcr(pageText[start..end]))
            {
                return true;
            }

            start = end;
        }

        return false;
    }

    private static bool IsWordSeparator(char c) =>
        c is ' ' or '\t' or '\r' or '\n' or '\f';

    public static PdfPageSplit SplitPdfPages(IReadOnlyList<PdfPageText> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        var embeddable = new List<PdfPageText>();
        var ocrPageNumbers = new List<int>();
        foreach (var page in pages)
        {
            if (IsOcrPage(page.Text))
            {
                ocrPageNumbers.Add(page.PageNumber);
            }
            else
            {
                embeddable.Add(page);
            }
        }

        if (embeddable.Count == 0)
        {
            return new PdfPageSplit(string.Empty, embeddable, ocrPageNumbers);
        }

        for (var index = 0; index < embeddable.Count - 1; index++)
        {
            var page = embeddable[index];
            embeddable[index] = new PdfPageText(page.PageNumber, page.Text + '\f');
        }

        var builder = new System.Text.StringBuilder();
        foreach (var page in embeddable)
        {
            builder.Append(page.Text);
        }

        return new PdfPageSplit(builder.ToString(), embeddable, ocrPageNumbers);
    }

    public static IReadOnlyList<PdfPatch> Plan(
        string title,
        string joinedDocument,
        IReadOnlyList<PdfPageText>? pages,
        int prefixTokenCount,
        IReadOnlyList<int>? tokenEnds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(joinedDocument);
        ArgumentNullException.ThrowIfNull(tokenEnds);
        ArgumentOutOfRangeException.ThrowIfNegative(prefixTokenCount);
        return PlanCore(joinedDocument, pages, prefixTokenCount, [.. tokenEnds]);
    }

    private static List<PdfPatch> PlanCore(
        string document,
        IReadOnlyList<PdfPageText>? pages,
        int prefixTokenCount,
        int[] ends)
    {
        var pageStarts = PageStartOffsets(pages);
        var sentenceStarts = SentenceStarts(document, ends);
        var patches = new List<PdfPatch>();
        var ordinal = 0;
        var startChar = 0;
        var documentLength = document.Length;
        var endCount = ends.Length;
        var aim = Math.Max(1, PatchTargetTokens - prefixTokenCount);
        while (startChar < documentLength)
        {
            var startToken = LargestToken(ends, startChar) + 1;
            if (endCount == 0 || startToken + aim >= endCount)
            {
                patches.Add(BuildPatch(document, pages, pageStarts, ordinal, startChar, documentLength));
                break;
            }

            var lo = Math.Max(startToken + aim - LookbackTokens, startToken + 1);
            var hi = Math.Min(startToken + aim, endCount - 1);
            var (cutChar, cutToken) = CutPiece(document, ends, lo, hi, startToken, prefixTokenCount, endCount);
            patches.Add(BuildPatch(document, pages, pageStarts, ordinal, startChar, cutChar));
            ordinal++;
            startChar = OverlapStart(sentenceStarts, cutToken, cutChar);
        }

        return patches;
    }

    private static (int CutChar, int CutToken) CutPiece(
        string document,
        int[] ends,
        int lo,
        int hi,
        int startToken,
        int prefixTokenCount,
        int endCount)
    {
        var boundary = FindBoundary(document, ends, lo, hi);
        var reach = hi;
        while (boundary < 0 && reach < endCount - 1 && !EndsPatch(reach - startToken + prefixTokenCount))
        {
            reach = Math.Min(reach + LookbackTokens, endCount - 1);
            boundary = FindBoundary(document, ends, lo, reach);
        }

        if (boundary < 0)
        {
            boundary = OverflowBoundary(document, ends, lo, reach);
        }

        if (boundary < 0)
        {
            var fallbackToken = Math.Max(reach, startToken + 1);
            return (ends[fallbackToken], fallbackToken);
        }

        var cutToken = LargestToken(ends, boundary);
        return (ends[cutToken], cutToken);
    }

    private static int FindBoundary(string document, int[] ends, int lo, int hi)
    {
        var bestPos = -1;
        var bestClass = 0;
        for (var position = ends[hi]; position >= ends[lo]; position--)
        {
            var cls = BoundaryClass(document, position);
            if (cls > bestClass)
            {
                bestClass = cls;
                bestPos = position;
                if (cls == ParagraphClass)
                {
                    break;
                }
            }
        }

        return bestPos;
    }

    private static int OverflowBoundary(string document, int[] ends, int lo, int reach)
    {
        var whitespace = -1;
        for (var position = ends[reach]; position >= ends[lo]; position--)
        {
            if (IsSentenceBoundary(document, position))
            {
                return position;
            }

            if (whitespace < 0 && position < document.Length && char.IsWhiteSpace(document[position]))
            {
                whitespace = position;
            }
        }

        return whitespace;
    }

    private static int BoundaryClass(string document, int position)
    {
        if (position < 1 || position >= document.Length)
        {
            return 0;
        }

        if (position >= 2 && document[position - 1] == '\n' && document[position - 2] == '\n')
        {
            return ParagraphClass;
        }

        if (document[position - 1] == '\f')
        {
            return PageBreakClass;
        }

        if (document[position - 1] == '\n' && IsHeadingLine(document, position))
        {
            return HeadingClass;
        }

        if (IsSentenceBoundary(document, position))
        {
            return SentenceClass;
        }

        if (document[position - 1] == '\n')
        {
            return LineBreakClass;
        }

        if (document[position - 1] == ArabicSemicolon)
        {
            return ArabicSemicolonClass;
        }

        if (document[position - 1] == ArabicComma)
        {
            return ArabicCommaClass;
        }

        if (char.IsWhiteSpace(document[position]))
        {
            return WhitespaceClass;
        }

        return 0;
    }

    private static bool IsSentenceBoundary(string document, int position)
    {
        if (position == 0 || position >= document.Length)
        {
            return false;
        }

        if (!char.IsWhiteSpace(document[position]))
        {
            return false;
        }

        var mark = document[position - 1];
        if (mark is not ('.' or '!' or '?' or '\u061F' or '\u2026'))
        {
            return false;
        }

        if (IsAbbreviation(document, position - 1))
        {
            return false;
        }

        var next = position;
        while (next < document.Length && char.IsWhiteSpace(document[next]))
        {
            next++;
        }

        if (next >= document.Length)
        {
            return false;
        }

        return document[next] is >= 'A' and <= 'Z' || IsArabicLetter(document[next]);
    }

    private static bool IsArabicLetter(char c) =>
        c is >= '\u0600' and <= '\u06FF' or >= '\u0750' and <= '\u077F' or >= '\u08A0' and <= '\u08FF';

    private static bool IsAbbreviation(string document, int markPosition)
    {
        var start = markPosition;
        while (start > 0 && !char.IsWhiteSpace(document[start - 1]))
        {
            start--;
        }

        var word = document[start..(markPosition + 1)];
        return Array.Exists(Abbreviations, candidate => candidate.AsSpan().Equals(word, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsHeadingLine(string document, int position)
    {
        if (document[position - 1] != '\n')
        {
            return false;
        }

        var index = position;
        while (index < document.Length && (document[index] == ' ' || document[index] == '\t'))
        {
            index++;
        }

        var hashes = 0;
        while (index < document.Length && document[index] == '#')
        {
            index++;
            hashes++;
        }

        return hashes >= 1 && hashes <= 6 && index < document.Length && document[index] == ' ';
    }

    private static int LargestToken(int[] ends, int position)
    {
        var low = 0;
        var high = ends.Length - 1;
        var result = -1;
        while (low <= high)
        {
            var mid = low + ((high - low) / 2);
            if (ends[mid] <= position)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }

    private static int OverlapStart(List<(int Position, int Token)> sentenceStarts, int cutToken, int fallback)
    {
        var low = cutToken - MaxOverlapTokens;
        var high = cutToken - OverlapTokens;
        var bestToken = -1;
        var bestPosition = -1;
        foreach (var (position, token) in sentenceStarts)
        {
            if (token < low || token > high)
            {
                continue;
            }

            if (token > bestToken)
            {
                bestToken = token;
                bestPosition = position;
            }
        }

        if (bestToken >= 0)
        {
            return bestPosition;
        }

        for (var index = sentenceStarts.Count - 1; index >= 0; index--)
        {
            var (position, token) = sentenceStarts[index];
            if (token < cutToken)
            {
                return position;
            }
        }

        return fallback;
    }

    private static List<(int Position, int Token)> SentenceStarts(string document, int[] ends)
    {
        var starts = new List<(int Position, int Token)>();
        for (var index = 0; index < document.Length; index++)
        {
            var mark = document[index];
            if (mark is not ('.' or '!' or '?' or '\u061F' or '\u2026'))
            {
                continue;
            }

            if (index + 1 >= document.Length || !IsSentenceBoundary(document, index + 1))
            {
                continue;
            }

            var next = index + 2;
            while (next < document.Length && char.IsWhiteSpace(document[next]))
            {
                next++;
            }

            if (next >= document.Length)
            {
                continue;
            }

            starts.Add((next, LargestToken(ends, next) + 1));
        }

        return starts;
    }

    private static int[] PageStartOffsets(IReadOnlyList<PdfPageText>? pages)
    {
        if (pages is null || pages.Count == 0)
        {
            return [];
        }

        var offsets = new int[pages.Count];
        var offset = 0;
        for (var index = 0; index < pages.Count; index++)
        {
            offsets[index] = offset;
            offset += pages[index].Text.Length;
        }

        return offsets;
    }

    private static (int PageStart, int PageEnd) PageSpan(
        IReadOnlyList<PdfPageText>? pages,
        int[] pageStarts,
        int charStart,
        int charEnd)
    {
        if (pages is null || pages.Count == 0)
        {
            return (0, 0);
        }

        var last = pages.Count - 1;
        var start = last;
        while (start > 0 && pageStarts[start] > charStart)
        {
            start--;
        }

        var end = last;
        while (end > 0 && pageStarts[end] > charEnd)
        {
            end--;
        }

        return (pages[start].PageNumber, pages[end].PageNumber);
    }

    private static PdfPatch BuildPatch(
        string document,
        IReadOnlyList<PdfPageText>? pages,
        int[] pageStarts,
        int ordinal,
        int charStart,
        int charEnd)
    {
        var (pageStart, pageEnd) = PageSpan(pages, pageStarts, charStart, charEnd);
        return new PdfPatch(document[charStart..charEnd], ordinal, pageStart, pageEnd, charStart, charEnd);
    }
}
