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


## src/Recall.Core/Scope/GitIgnoreMatcher.cs

```csharp
namespace Recall.Core.Scope;
public sealed record GitIgnoreFile(string DirectoryRelative, IReadOnlyList<string> Lines);
public readonly record struct GitIgnorePattern(string Body, bool Negated, bool DirectoryOnly, bool Anchored);
public enum GitIgnoreDecision
public static class GitIgnoreMatcher
    public static bool TryParse(string line, out GitIgnorePattern pattern)
    public static GitIgnoreDecision LastDecision(string relativePath, bool isDirectory, IReadOnlyList<GitIgnoreFile> files)
    public static bool IsIgnored(string relativePath, IReadOnlyList<GitIgnoreFile> files)
    public static IReadOnlyList<GitIgnoreFile> LoadDirectoryFiles(string grantRoot, string directory)
    if (string.IsNullOrWhiteSpace(line))
    if (line.StartsWith("\\#", StringComparison.Ordinal) || line.StartsWith("\\!", StringComparison.Ordinal))
    if (string.IsNullOrEmpty(body))
    if (body.EndsWith("/", StringComparison.Ordinal) && body.Length > 0)
    if (body.StartsWith("/", StringComparison.Ordinal))
    if (line[0] == '#')
    if (rest.StartsWith("!", StringComparison.Ordinal))
    if (string.IsNullOrEmpty(rest))
    if (rest.EndsWith("/", StringComparison.Ordinal))
    if (rest.StartsWith("/", StringComparison.Ordinal))
    if (string.IsNullOrEmpty(relativePath) || files is null || files.Count == 0)
    if (string.IsNullOrEmpty(candidate))
    if (string.IsNullOrEmpty(dir))
    else if (candidate.Equals(dir, StringComparison.Ordinal))
    else if (candidate.StartsWith(dir + "/", StringComparison.Ordinal))
    if (file.Lines is null)
    if (!TryParse(line, out var pattern))
    if (pattern.DirectoryOnly && !isDirectory)
    if (pattern.Anchored)
    if (!matched)
    if (matched)
    if (LastDecision(prefix, true, files) == GitIgnoreDecision.Ignore)
    if (string.IsNullOrWhiteSpace(grantRoot) || string.IsNullOrWhiteSpace(directory))
    if (!ScopeGrantPolicy.IsInsideGrant(grantRoot, directory))
    if (grantNorm.Equals(dirNorm, StringComparison.OrdinalIgnoreCase))
    else if (ScopeGrantPolicy.IsChildPath(grantNorm, dirNorm))
    if (File.Exists(ignorePath))
    if (File.Exists(excludePath))
    if (c == '*')
    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
    if (after == '/')
    if (beforeSlash && (i + 2 >= pattern.Length))
    if (c == '?')
    if (c == '[')
    if (close < 0)
    if (content.Length == 0)
    if (content[0] == '!')
    if (c == '\\')
    if ("+()^$.{}|".IndexOf(c, StringComparison.Ordinal) >= 0)
    catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
```



## src/Recall.Core/Scope/FileAdmission.cs

```csharp
namespace Recall.Core.Scope;
public enum FileAdmissionResult
public static class FileAdmission
    if (string.IsNullOrWhiteSpace(fileName))
    if (IsSecret(fileName))
    if (IsDatabase(fileName))
    if (IsLockFile(fileName))
    if (user.FileNames.Any(candidate => string.Equals(candidate, fileName, StringComparison.OrdinalIgnoreCase)))
    if (!string.IsNullOrEmpty(extension)
    if (IsMinified(fileName))
    if (gitIgnored)
    if (IsCodeFile(fileName))
    if (kinds.HasFlag(GrantFileKinds.Code))
    if (GrantTextFiles.IsTextFile(fileName))
    if (kinds.HasFlag(GrantFileKinds.Text))
    if (string.IsNullOrWhiteSpace(folderName))
    if (BuiltInSkips.ContainsFolder(folderName))
    if (user.FolderNames.Any(candidate => string.Equals(candidate, folderName, StringComparison.OrdinalIgnoreCase)))
    if (!string.IsNullOrEmpty(extension))
    if (BuiltInSkips.ContainsSecretExtension(withoutDot))
    if (BuiltInSkips.ContainsSecretName(fileName))
    if (fileName.Equals("id_rsa", StringComparison.OrdinalIgnoreCase)
    if (fileName.StartsWith(".env", StringComparison.OrdinalIgnoreCase)
    if (!fileName.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase)
    if (bare.Equals("secret", StringComparison.OrdinalIgnoreCase)
    if (string.IsNullOrEmpty(extension))
    if (!string.IsNullOrEmpty(extension) && BuiltInSkips.ContainsCodeExtension(extension))
    if (BuiltInSkips.ContainsExactCodeName(fileName))
```



## src/Recall.Core/Scope/BuiltInSkips.cs

```csharp
namespace Recall.Core.Scope;
public static class BuiltInSkips
    if (string.IsNullOrEmpty(name))
    if (string.IsNullOrEmpty(extensionWithDot))
    if (string.IsNullOrEmpty(fileName))
    if (string.IsNullOrEmpty(extensionWithoutDot))
```

