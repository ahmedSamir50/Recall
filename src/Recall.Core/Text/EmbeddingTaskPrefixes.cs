using Recall.Core.Faults;

namespace Recall.Core.Text;

public static class EmbeddingTaskPrefixes
{
    public const string SearchTask = "task: search result | query: ";

    public const string ClusterTask = "task: clustering | query: ";

    public const string ClassifyTask = "task: classification | query: ";

    public static string WrapSearchQuery(string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (query.StartsWith(SearchTask, StringComparison.Ordinal))
        {
            throw new RecallFaultException(RecallFault.SearchPrefixAppliedTwice);
        }

        return SearchTask + query;
    }

    public static string WrapTitledDocument(string title, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (content.StartsWith("title: ", StringComparison.Ordinal) && content.Contains(" | text: ", StringComparison.Ordinal))
        {
            throw new RecallFaultException(RecallFault.DocumentPrefixAppliedTwice);
        }

        var safeTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        if (safeTitle.Length == 0)
        {
            return "text: " + content;
        }

        return "title: " + safeTitle + " | text: " + content;
    }
}
