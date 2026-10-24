using Recall.Core.Faults;

namespace Recall.Core.Text;

public static class EmbeddingTaskPrefixes
{
    public const string SearchTask = "task: search result | query: ";

    public const string ClusterTask = "task: clustering | query: ";

    public const string ClassifyTask = "task: classification | query: ";

    public const string TitleMarker = "title: ";

    public const string TextJoiner = " | text: ";

    public const string TextOnlyMarker = "text: ";

    public static string WrapSearchQuery(string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (query.StartsWith(SearchTask, StringComparison.Ordinal))
        {
            throw new RecallFaultException(RecallFault.SearchPrefixAppliedTwice);
        }

        return SearchTask + query;
    }

    public static string TitledPrefix(string title)
    {
        var safeTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        if (safeTitle.Length == 0)
        {
            return TextOnlyMarker;
        }

        return TitleMarker + safeTitle + TextJoiner;
    }

    public static string WrapTitledDocument(string title, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (content.StartsWith(TitleMarker, StringComparison.Ordinal) && content.Contains(TextJoiner, StringComparison.Ordinal))
        {
            throw new RecallFaultException(RecallFault.DocumentPrefixAppliedTwice);
        }

        var safeTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        if (safeTitle.Length == 0)
        {
            return TextOnlyMarker + content;
        }

        return TitleMarker + safeTitle + TextJoiner + content;
    }
}
