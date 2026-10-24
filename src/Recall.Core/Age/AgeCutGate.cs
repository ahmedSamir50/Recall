using Recall.Core.Faults;
using Recall.Core.Indexing;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;

namespace Recall.Core.Age;

public static class AgeCutGate
{
    public static bool Passes(Modality modality, IReadOnlyList<AgeCutItem> items)
    {
        if (modality == Modality.Text)
        {
            throw new RecallFaultException(RecallFault.AgeCutTextForbidden);
        }

        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            return false;
        }

        foreach (var item in items)
        {
            if (item.Stored768.Length != MatryoshkaVectorMath.NativeWidth || item.Query768.Length != MatryoshkaVectorMath.NativeWidth)
            {
                throw new RecallFaultException(RecallFault.EmbeddingWidthUnexpected);
            }
        }

        var fullRecall = RecallAt10(items, useCut: false);
        var cutRecall = RecallAt10(items, useCut: true);
        return Math.Abs(fullRecall - cutRecall) <= 5d;
    }

    private static double RecallAt10(IReadOnlyList<AgeCutItem> items, bool useCut)
    {
        var hits = 0;
        for (var queryIndex = 0; queryIndex < items.Count; queryIndex++)
        {
            var queryItem = items[queryIndex];
            var query = VectorFor(queryItem.Query768, useCut);
            var ranked = new List<(int Id, double Cosine)>(items.Count);
            for (var storedIndex = 0; storedIndex < items.Count; storedIndex++)
            {
                var storedItem = items[storedIndex];
                var stored = VectorFor(storedItem.Stored768, useCut);
                ranked.Add((storedItem.Id, Cosine(query, stored)));
            }

            ranked.Sort(static (left, right) =>
            {
                var byCosine = right.Cosine.CompareTo(left.Cosine);
                return byCosine != 0 ? byCosine : left.Id.CompareTo(right.Id);
            });

            var rank = ranked.FindIndex(entry => entry.Id == queryItem.Id);
            if (rank >= 0 && rank < 10)
            {
                hits++;
            }
        }

        return 100d * hits / items.Count;
    }

    private static float[] VectorFor(float[] source768, bool useCut)
    {
        if (!useCut)
        {
            var copy = new float[source768.Length];
            source768.CopyTo(copy, 0);
            MatryoshkaVectorMath.L2Normalize(copy);
            return copy;
        }

        return MatryoshkaVectorMath.CutAndNormalize(source768, MediaCollections.OldWidth);
    }

    private static double Cosine(float[] left, float[] right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return 0d;
        }

        var sum = 0d;
        for (var i = 0; i < left.Length; i++)
        {
            sum += left[i] * (double)right[i];
        }

        return sum;
    }
}
