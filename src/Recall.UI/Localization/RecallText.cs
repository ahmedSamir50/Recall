using System.Globalization;
using System.Resources;

namespace Recall.UI.Localization;

public static class RecallText
{
    public const string ResourceBaseName = "Recall.UI.Resources.Strings";

    private static readonly ResourceManager Manager = new(ResourceBaseName, typeof(RecallText).Assembly);

    public static string Get(string key, CultureInfo? culture = null)
    {
        return Manager.GetString(key, culture ?? CultureInfo.CurrentUICulture) ?? key;
    }

    public static IReadOnlyList<string> AllKeys()
    {
        var set = Manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        if (set is null)
        {
            return [];
        }

        return set.Cast<System.Collections.DictionaryEntry>()
            .Select(entry => entry.Key.ToString()!)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
    }
}
