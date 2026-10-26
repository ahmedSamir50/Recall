namespace Recall.Core.Ocr;

public static class CtcDecoder
{
    public static (string Text, double Confidence) Decode(float[] logits, int timeSteps, int classes, IReadOnlyList<string> dict, bool visualArabic)
    {
        ArgumentNullException.ThrowIfNull(logits);
        ArgumentNullException.ThrowIfNull(dict);
        if (timeSteps < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSteps));
        }

        if (classes < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(classes));
        }

        if (logits.Length != timeSteps * classes)
        {
            throw new ArgumentException("Logits length must equal timeSteps times classes.", nameof(logits));
        }

        if (classes != dict.Count + 1 && classes != dict.Count + 2)
        {
            throw new InvalidOperationException("OCR class count does not match the character dict.");
        }

        var ids = new List<int>();
        var confidences = new List<double>();
        for (var t = 0; t < timeSteps; t++)
        {
            var offset = t * classes;
            var maxIndex = 0;
            var maxLogit = logits[offset];
            for (var c = 1; c < classes; c++)
            {
                var value = logits[offset + c];
                if (value > maxLogit)
                {
                    maxLogit = value;
                    maxIndex = c;
                }
            }

            var expSum = 0d;
            for (var c = 0; c < classes; c++)
            {
                expSum += Math.Exp(logits[offset + c] - maxLogit);
            }

            var prob = 1d / expSum;
            if (maxIndex != 0)
            {
                confidences.Add(prob);
            }

            ids.Add(maxIndex);
        }

        var collapsed = new List<int>();
        var last = -1;
        foreach (var id in ids)
        {
            if (id == 0)
            {
                last = -1;
                continue;
            }

            if (id == last)
            {
                continue;
            }

            collapsed.Add(id);
            last = id;
        }

        if (collapsed.Count == 0)
        {
            return (string.Empty, 0);
        }

        var chars = new List<string>();
        foreach (var id in collapsed)
        {
            var index = id - 1;
            if (index >= 0 && index < dict.Count)
            {
                chars.Add(dict[index]);
            }
            else if (index == dict.Count)
            {
                chars.Add(" ");
            }
        }

        var text = string.Concat(chars);
        if (visualArabic)
        {
            text = ArabicVisualOrder.ToLogical(text);
        }

        var confidence = confidences.Count == 0 ? 0 : confidences.Average();
        return (text, confidence);
    }
}
