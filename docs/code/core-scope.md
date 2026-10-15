## src/Recall.Core/Scope/ScopeGrantPolicy.cs

```csharp
namespace Recall.Core.Scope;
public static class ScopeGrantPolicy
    public static bool IsChildPath(string parentPath, string childPath)
    public static bool IsInsideGrant(string grantPath, string filePath)
    public static string Normalize(string path)
        if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(childPath))
        if (parent.Equals(child, StringComparison.OrdinalIgnoreCase))
        if (!child.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
        if (string.IsNullOrWhiteSpace(grantPath) || string.IsNullOrWhiteSpace(filePath))
        if (file.Equals(grant, StringComparison.OrdinalIgnoreCase))
```


## src/Recall.Core/Scope/ScopeGrantService.cs

```csharp
namespace Recall.Core.Scope;
public sealed class ScopeGrantService : IScopeManagerService
    public ScopeGrantService(IScopeGrantStore store)
    public IReadOnlyList<ScopeFolderGrant> ListGrants()
    public ScopeGrantAddResult TryAddGrant(ScopeFolderGrant grant)
    public void RemoveGrant(string path)
    public void SetGrantFileKinds(string path, GrantFileKinds kinds)
    public bool CanContinue()
        if (grants.Any(g => ScopeGrantPolicy.Normalize(g.Path).Equals(path, StringComparison.OrdinalIgnoreCase)))
            if (ScopeGrantPolicy.IsChildPath(existingPath, path))
        if (kinds == GrantFileKinds.None)
            throw new RecallFaultException(RecallFault.GrantFileKindsEmpty);
        if (index < 0)
```


## src/Recall.Core/Scope/ScopeFolderGrant.cs

```csharp
namespace Recall.Core.Scope;
public sealed record ScopeFolderGrant(
```

No branch.


## src/Recall.Core/Scope/ScopeGrantAddResult.cs

```csharp
namespace Recall.Core.Scope;
public enum ScopeGrantAddStatus
public sealed record ScopeGrantAddResult(ScopeGrantAddStatus Status, string? Message = null);
```

No branch.


## src/Recall.Core/Scope/GrantFileKinds.cs

```csharp
namespace Recall.Core.Scope;
public enum GrantFileKinds
```

No branch.


## src/Recall.Core/Scope/ContentUri.cs

```csharp
namespace Recall.Core.Scope;
public static class ContentUri
```

No branch.
