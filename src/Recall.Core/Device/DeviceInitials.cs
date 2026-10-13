using System.Globalization;

namespace Recall.Core.Device;

public static class DeviceInitials
{
    public static string FromDisplayName(string displayName)
    {
        var name = displayName.Trim();
        if (name.Length == 0)
        {
            return "?";
        }

        var parts = name.Split([' ', '.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{FirstGraphemeUpper(parts[0])}{FirstGraphemeUpper(parts[1])}";
        }

        return FirstGraphemeUpper(name);
    }

    private static string FirstGraphemeUpper(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        if (!enumerator.MoveNext())
        {
            return "?";
        }

        var grapheme = enumerator.GetTextElement();
        return grapheme.ToUpperInvariant();
    }
}
