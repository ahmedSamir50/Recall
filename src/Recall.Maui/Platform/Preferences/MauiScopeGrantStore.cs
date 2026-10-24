using Recall.Core.Ports;
using Recall.Core.Scope;
using System.Text.Json;

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

        var grants = JsonSerializer.Deserialize<List<ScopeFolderGrant>>(json) ?? [];
        return grants
            .Select(g => g.EnabledKinds == GrantFileKinds.None ? g with { EnabledKinds = GrantFileKinds.All } : g)
            .ToList();
    }

    public void Save(IReadOnlyList<ScopeFolderGrant> grants)
    {
        var json = JsonSerializer.Serialize(grants);
        Preferences.Default.Set(Key, json);
    }
}
