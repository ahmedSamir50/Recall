namespace Recall.Core.Scope;

public interface IOversizeFileSkipReport
{
    void Add(string path);

    void Clear();

    IReadOnlyList<string> Paths { get; }
}

public sealed class OversizeFileSkipReport : IOversizeFileSkipReport
{
    private readonly List<string> paths = [];

    public IReadOnlyList<string> Paths => paths;

    public void Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!paths.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            paths.Add(path);
        }
    }

    public void Clear() => paths.Clear();
}
