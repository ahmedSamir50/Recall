namespace Recall.Core.Ocr;

public static class OcrGoldenPages
{
    public static IReadOnlyList<string> SplitEqual(string text, int count)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, null);
        }

        if (text.Length < count)
        {
            throw new ArgumentException("Source is shorter than the page count.", nameof(text));
        }

        var sliceLength = text.Length / count;
        var slices = new List<string>(count);
        for (var index = 0; index < count - 1; index++)
        {
            slices.Add(text.Substring(index * sliceLength, sliceLength));
        }

        slices.Add(text.Substring((count - 1) * sliceLength));
        return slices;
    }
}
