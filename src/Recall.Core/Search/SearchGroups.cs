namespace Recall.Core.Search;

public enum SearchGroupKind
{
    Text = 0,
    Image = 1,
    Video = 2,
    Audio = 3,
}

public readonly record struct SearchResultGroup(string GroupKey, IReadOnlyList<TextFindCard> Cards);

public static class SearchGroups
{
    private static readonly (SearchGroupKind Kind, string GroupKey)[] Order =
    [
        (SearchGroupKind.Text, "Search_Group_Text"),
        (SearchGroupKind.Image, "Search_Group_Image"),
        (SearchGroupKind.Video, "Search_Group_Video"),
        (SearchGroupKind.Audio, "Search_Group_Audio"),
    ];

    public static IReadOnlyList<SearchResultGroup> Split(IReadOnlyList<TextFindCard> cards)
    {
        var buckets = new Dictionary<SearchGroupKind, List<TextFindCard>>();
        foreach (var card in cards)
        {
            var kind = TextFindCardKind.KindFromPath(card.SourcePath);
            if (!buckets.TryGetValue(kind, out var list))
            {
                list = [];
                buckets[kind] = list;
            }

            list.Add(card);
        }

        var groups = new List<SearchResultGroup>();
        foreach (var (kind, groupKey) in Order)
        {
            if (!buckets.TryGetValue(kind, out var list) || list.Count == 0)
            {
                continue;
            }

            groups.Add(new SearchResultGroup(groupKey, list));
        }

        return groups;
    }
}