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
                AppendLine(builder, line, page);
                line.Clear();
            }

            if (!string.IsNullOrEmpty(letter.Value))
            {
                line.Add(letter);
            }
        }

        AppendLine(builder, line, page);
        return NormalizeNewlines(builder.ToString());
    }

    internal static string NormalizeNewlines(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static void AppendLine(StringBuilder builder, List<Letter> line, Page page)
    {
        if (line.Count == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        if (!LineHasPresentation(line))
        {
            foreach (var letter in line)
            {
                if (IsWhitespace(letter))
                {
                    builder.Append(' ');
                }
                else
                {
                    builder.Append(letter.Value);
                }
            }

            return;
        }

        var runs = new List<TextRun>();
        foreach (var word in WordsOnLine(page, line))
        {
            if (string.IsNullOrEmpty(word.Text))
            {
                continue;
            }

            var kind = IsRtlWord(word) ? RunKind.Rtl : RunKind.Ltr;
            var run = new TextRun(kind, word.BoundingBox.Left);
            foreach (var letter in word.Letters)
            {
                run.Add(letter);
            }

            run.Finish();
            runs.Add(run);
        }

        OrderRuns(runs, page, line);

        for (var index = 0; index < runs.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(runs[index].Text);
        }
    }

    private static bool LineHasPresentation(List<Letter> line)
    {
        foreach (var letter in line)
        {
            if (IsWhitespace(letter))
            {
                continue;
            }

            foreach (var rune in letter.Value.EnumerateRunes())
            {
                if (rune.Value is >= 0xFB50 and <= 0xFDFF or >= 0xFE70 and <= 0xFEFC)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void OrderRuns(List<TextRun> runs, Page page, List<Letter> line)
    {
        var hasLtr = false;
        var hasRtl = false;
        var visualRtl = false;
        foreach (var run in runs)
        {
            if (run.Kind == RunKind.Ltr)
            {
                hasLtr = true;
            }
            else
            {
                hasRtl = true;
                visualRtl |= run.Visual;
            }
        }

        if (hasLtr && hasRtl)
        {
            var ltrRight = double.MinValue;
            var rtlLeft = double.MaxValue;
            foreach (var run in runs)
            {
                if (run.Kind == RunKind.Ltr)
                {
                    ltrRight = Math.Max(ltrRight, run.Right);
                }
                else
                {
                    rtlLeft = Math.Min(rtlLeft, run.Left);
                }
            }

            var rtlRightOfLtr = rtlLeft > ltrRight + 0.5;
            var minX = double.MaxValue;
            foreach (var letter in line)
            {
                minX = Math.Min(minX, letter.GlyphRectangle.Left);
            }

            var rightAlignedBlock = minX > page.Width / 2d;
            runs.Sort(rtlRightOfLtr || rightAlignedBlock
                ? static (left, right) => right.Left.CompareTo(left.Left)
                : static (left, right) => left.Left.CompareTo(right.Left));
            return;
        }

        if (visualRtl)
        {
            runs.Reverse();
        }
    }

    private static IEnumerable<Word> WordsOnLine(Page page, List<Letter> line)
    {
        var lineBottom = line[0].GlyphRectangle.Bottom;
        var height = 0d;
        foreach (var letter in line)
        {
            height = Math.Max(height, letter.GlyphRectangle.Height);
        }

        height = Math.Max(height, 2);
        foreach (var word in page.GetWords())
        {
            if (Math.Abs(word.BoundingBox.Bottom - lineBottom) <= height * 0.5)
            {
                yield return word;
            }
        }
    }

    private static bool IsRtlWord(Word word)
    {
        foreach (var letter in word.Letters)
        {
            if (ContainsArabicCodePoint(letter.Value))
            {
                return true;
            }
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
    }

    private sealed class TextRun
    {
        private readonly StringBuilder pieces = new();

        public TextRun(RunKind kind, double sortLeft)
        {
            Kind = kind;
            Left = sortLeft;
            Right = sortLeft;
        }

        public RunKind Kind { get; }

        public string Text { get; private set; } = string.Empty;

        public bool Visual { get; private set; }

        public double Left { get; private set; }

        public double Right { get; private set; }

        private double firstX;

        private double lastX;

        private bool hasX;

        public void Add(Letter letter)
        {
            var box = letter.GlyphRectangle;
            if (!hasX)
            {
                firstX = box.Left;
                hasX = true;
            }

            Left = Math.Min(Left, box.Left);
            Right = Math.Max(Right, box.Right);

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
