namespace Recall.Core.Scope;

public interface IRemoteFileSkipReport
{
    void Add(string path);

    void Clear();

    int SkippedCount { get; }

    IReadOnlyList<string> Paths { get; }
}

public sealed class RemoteFileSkipReport : IRemoteFileSkipReport
{
    public const int ShownCap = 20;

    private readonly List<string> shown = [];
    private int skippedCount;

    public int SkippedCount => skippedCount;

    public IReadOnlyList<string> Paths => shown;

    public void Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        skippedCount++;
        if (shown.Count < ShownCap && !shown.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            shown.Add(path);
        }
    }

    public void Clear()
    {
        shown.Clear();
        skippedCount = 0;
    }
}
