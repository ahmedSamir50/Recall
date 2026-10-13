using System.Text.Json;
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Maui.Platform;

public sealed class MauiScopeGrantStore : IScopeGrantStore
{
    private const string Key = "recall.scope.grants";

    public IReadOnlyList<ScopeFolderGrant> List()
    {
        var json = Preferences.Default.Get(Key, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<ScopeFolderGrant>>(json) ?? [];
    }

    public void Save(IReadOnlyList<ScopeFolderGrant> grants)
    {
        var json = JsonSerializer.Serialize(grants);
        Preferences.Default.Set(Key, json);
    }
}
