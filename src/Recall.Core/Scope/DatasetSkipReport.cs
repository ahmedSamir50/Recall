namespace Recall.Core.Scope;

public interface IDatasetSkipReport
{
    void Add(string folderPath);

    void Clear();

    IReadOnlyList<string> Paths { get; }
}

public sealed class DatasetSkipReport : IDatasetSkipReport
{
    private readonly List<string> paths = [];

    public IReadOnlyList<string> Paths => paths;

    public void Add(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        if (!paths.Contains(folderPath, StringComparer.OrdinalIgnoreCase))
        {
            paths.Add(folderPath);
        }
    }

    public void Clear() => paths.Clear();
}
