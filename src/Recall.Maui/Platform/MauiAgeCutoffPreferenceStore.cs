using Recall.Core.Ports;
using Recall.Core.Vocabulary;

namespace Recall.Maui.Platform;

public sealed class MauiAgeCutoffPreferenceStore : IAgeCutoffPreferenceStore
{
    private const string Key = "recall.preferences.ageCutoff";

    public AgeCutoffYears Get()
    {
        var raw = Preferences.Default.Get(Key, (int)AgeCutoffYears.Two);
        return Enum.IsDefined(typeof(AgeCutoffYears), raw)
            ? (AgeCutoffYears)raw
            : AgeCutoffYears.Two;
    }

    public void Set(AgeCutoffYears years) => Preferences.Default.Set(Key, (int)years);
}
