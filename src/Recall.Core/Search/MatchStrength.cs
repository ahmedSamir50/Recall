namespace Recall.Core.Search;

public static class MatchStrength
{
    public static int? Percent(double similarity) =>
        similarity is >= 0 and <= 1 ? (int)Math.Round(similarity * 100) : null;
}