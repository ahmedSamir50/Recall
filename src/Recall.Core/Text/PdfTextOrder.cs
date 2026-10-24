using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Recall.Core.Text;

internal static class PdfTextOrder
{
    public static string Logical(Page page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (page.Letters.Count == 0 || !ContainsArabic(page.Letters))
        {
            return NormalizeNewlines(ContentOrderTextExtractor.GetText(page, addDoubleNewline: true));
        }

        var builder = new StringBuilder();
        var line = new List<Letter>();
        foreach (var letter in page.Letters)
        {
            if (line.Count > 0 && IsNewLine(line[0], letter))
            {
                AppendLine(builder, line);
                line.Clear();
            }

            if (!string.IsNullOrEmpty(letter.Value))
            {
                line.Add(letter);
            }
        }

        AppendLine(builder, line);
        return NormalizeNewlines(builder.ToString());
    }

    internal static string NormalizeNewlines(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static void AppendLine(StringBuilder builder, List<Letter> line)
    {
        if (line.Count == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        var gapThreshold = GapThreshold(line);
        var runs = new List<TextRun>();
        TextRun? current = null;
        Letter? previousGlyph = null;
        foreach (var letter in line)
        {
            if (IsWhitespace(letter))
            {
                Flush(runs, ref current);
                runs.Add(TextRun.Space());
                previousGlyph = null;
                continue;
            }

            if (previousGlyph is not null
                && IsRtl(previousGlyph.Value)
                && IsRtl(letter.Value)
                && NeedsGapSpace(previousGlyph, letter, gapThreshold))
            {
                Flush(runs, ref current);
                runs.Add(TextRun.Space());
            }

            var kind = IsRtl(letter.Value) ? RunKind.Rtl : RunKind.Ltr;
            if (current is null || current.Kind != kind)
            {
                Flush(runs, ref current);
                current = new TextRun(kind);
            }

            current.Add(letter);
            previousGlyph = letter;
        }

        Flush(runs, ref current);
        if (ShouldReverseMixedRuns(runs))
        {
            runs.Reverse();
        }

        foreach (var run in runs)
        {
            builder.Append(run.Text);
        }
    }

    private static double GapThreshold(List<Letter> line)
    {
        var totalWidth = 0d;
        var count = 0;
        foreach (var letter in line)
        {
            if (IsWhitespace(letter))
            {
                continue;
            }

            totalWidth += letter.GlyphRectangle.Width;
            count++;
        }

        if (count == 0)
        {
            return 1d;
        }

        return Math.Max(totalWidth / count * 0.35, 1d);
    }

    private static bool NeedsGapSpace(Letter previous, Letter next, double gapThreshold)
    {
        var gap = next.GlyphRectangle.Left - previous.GlyphRectangle.Right;
        return gap > gapThreshold;
    }

    private static void Flush(List<TextRun> runs, ref TextRun? current)
    {
        if (current is null)
        {
            return;
        }

        current.Finish();
        runs.Add(current);
        current = null;
    }

    private static bool ShouldReverseMixedRuns(IReadOnlyList<TextRun> runs)
    {
        var hasLtr = false;
        var hasRtl = false;
        var visualRtl = false;
        var ltrRight = double.MinValue;
        var rtlLeft = double.MaxValue;
        foreach (var run in runs)
        {
            if (run.Kind == RunKind.Space)
            {
                continue;
            }

            if (run.Kind == RunKind.Ltr)
            {
                hasLtr = true;
                ltrRight = Math.Max(ltrRight, run.Right);
            }
            else
            {
                hasRtl = true;
                rtlLeft = Math.Min(rtlLeft, run.Left);
                visualRtl |= run.Visual;
            }
        }

        if (hasLtr && hasRtl)
        {
            if (rtlLeft > ltrRight + 0.5)
            {
                return true;
            }

            TextRun? first = null;
            TextRun? last = null;
            foreach (var run in runs)
            {
                if (run.Kind == RunKind.Space)
                {
                    continue;
                }

                first ??= run;
                last = run;
            }

            return first is not null && last is not null && first.Left > last.Left + 0.5;
        }

        return false;
    }

    private static bool IsNewLine(Letter current, Letter next)
    {
        var height = Math.Max(current.GlyphRectangle.Height, 2);
        return Math.Abs(next.GlyphRectangle.Bottom - current.GlyphRectangle.Bottom) > height * 0.5;
    }

#pragma warning disable S3267
    private static bool ContainsArabic(IReadOnlyList<Letter> letters)
    {
        foreach (var letter in letters)
        {
            if (ContainsArabicCodePoint(letter.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRtl(string value) => ContainsArabicCodePoint(value);

    private static bool ContainsArabicCodePoint(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            if (IsArabicCodePoint(rune.Value))
            {
                return true;
            }
        }

        return false;
    }
#pragma warning restore S3267

    private static bool IsWhitespace(Letter letter) =>
        letter.Value.Length > 0 && letter.Value.EnumerateRunes().All(Rune.IsWhiteSpace);

    private static bool IsArabicCodePoint(int code) =>
        code is >= 0x0600 and <= 0x06FF
            or >= 0x0750 and <= 0x077F
            or >= 0x08A0 and <= 0x08FF
            or >= 0xFB50 and <= 0xFDFF
            or >= 0xFE70 and <= 0xFEFC;

    private enum RunKind
    {
        Ltr = 0,
        Rtl = 1,
        Space = 2,
    }

    private sealed class TextRun
    {
        private readonly StringBuilder pieces = new();

        public TextRun(RunKind kind) => Kind = kind;

        public RunKind Kind { get; }

        public string Text { get; private set; } = string.Empty;

        public bool Visual { get; private set; }

        public double Left { get; private set; }

        public double Right { get; private set; }

        private double firstX;

        private double lastX;

        private bool hasX;

        public static TextRun Space()
        {
            var run = new TextRun(RunKind.Space);
            run.Text = " ";
            return run;
        }

        public void Add(Letter letter)
        {
            var box = letter.GlyphRectangle;
            if (!hasX)
            {
                firstX = box.Left;
                Left = box.Left;
                Right = box.Right;
                hasX = true;
            }
            else
            {
                Left = Math.Min(Left, box.Left);
                Right = Math.Max(Right, box.Right);
            }

            lastX = box.Left;
            if (ContainsPresentation(letter.Value))
            {
                Visual = true;
            }

            pieces.Append(letter.Value);
        }

#pragma warning disable S3267
        private static bool ContainsPresentation(string value)
        {
            foreach (var rune in value.EnumerateRunes())
            {
                if (rune.Value is >= 0xFB50 and <= 0xFDFF or >= 0xFE70 and <= 0xFEFC)
                {
                    return true;
                }
            }

            return false;
        }
#pragma warning restore S3267

        public void Finish()
        {
            if (Kind == RunKind.Space)
            {
                Text = " ";
                return;
            }

            var text = pieces.ToString();
            if (Kind == RunKind.Rtl && Visual && hasX && lastX + 0.5 >= firstX)
            {
                text = Reverse(text).Normalize(NormalizationForm.FormKC);
            }
            else if (Visual)
            {
                text = text.Normalize(NormalizationForm.FormKC);
            }

            Text = text;
        }

        private static string Reverse(string text)
        {
            var chars = text.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }
    }
}
