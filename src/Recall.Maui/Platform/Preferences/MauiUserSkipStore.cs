using Recall.Core.Ports;
using Recall.Core.Preferences;

namespace Recall.Maui.Platform;

public sealed class MauiUserSkipStore : IUserSkipStore
{
    private const string Key = "recall.skips.user";

    public UserSkipAdditions Load()
    {
        var json = Preferences.Default.Get(Key, string.Empty);
        return UserSkipJson.Deserialize(json);
    }

    public void Save(UserSkipAdditions additions)
    {
        var json = UserSkipJson.Serialize(additions);
        Preferences.Default.Set(Key, json);
    }
}
