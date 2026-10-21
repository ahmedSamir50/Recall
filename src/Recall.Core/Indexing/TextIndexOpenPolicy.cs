namespace Recall.Core.Indexing;

public enum TextIndexOpenKind
{
    Create = 0,
    Open = 1,
    Replace = 2,
}

public static class TextIndexOpenPolicy
{
    public static TextIndexOpenKind Decide(bool collectionDirectoryExists, bool hasFtsKeyVector)
    {
        if (!collectionDirectoryExists)
        {
            return TextIndexOpenKind.Create;
        }

        if (!hasFtsKeyVector)
        {
            return TextIndexOpenKind.Replace;
        }

        return TextIndexOpenKind.Open;
    }
}
