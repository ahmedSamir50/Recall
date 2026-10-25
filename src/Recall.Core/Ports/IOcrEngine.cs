namespace Recall.Core.Ports;

public interface IOcrEngine : IDisposable
{
    void RunPass(IReadOnlyList<string> paths);
}
