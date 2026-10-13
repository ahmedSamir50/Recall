namespace Recall.Core.Ports;

public interface ITextTokenCounter
{
    int CountTokens(string text);
}
