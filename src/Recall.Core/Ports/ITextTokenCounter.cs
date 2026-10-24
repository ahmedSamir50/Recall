namespace Recall.Core.Ports;

public interface ITextTokenCounter
{
    int CountTokens(string text);

    IReadOnlyList<int> TokenEnds(string text);
}
