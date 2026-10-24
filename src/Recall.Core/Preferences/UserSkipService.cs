using Recall.Core.Ports;
using Recall.Core.Scope;
using System.Text.Json;

namespace Recall.Core.Preferences;

public sealed record UserSkipAdditions(IReadOnlyList<string> Extensions, IReadOnlyList<string> FolderNames, IReadOnlyList<string> FileNames)
{
    public static readonly UserSkipAdditions Empty = new([], [], []);
}

public enum UserSkipKind
{
    Extension = 1,
    FolderName = 2,
    FileName = 3,
}

public enum UserSkipAddStatus
{
    Added = 0,
    Empty = 1,
    Duplicate = 2,
    BuiltIn = 3,
}

public static class UserSkipJson
{
    public static string Serialize(UserSkipAdditions additions)
    {
        ArgumentNullException.ThrowIfNull(additions);
        return JsonSerializer.Serialize(additions);
    }

    public static UserSkipAdditions Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return UserSkipAdditions.Empty;
        }

        UserSkipAdditions? document;
        try
        {
            document = JsonSerializer.Deserialize<UserSkipAdditions>(json);
        }
        catch (JsonException)
        {
            return UserSkipAdditions.Empty;
        }

        if (document is null)
        {
            return UserSkipAdditions.Empty;
        }

        return new UserSkipAdditions(
            document.Extensions ?? [],
            document.FolderNames ?? [],
            document.FileNames ?? []);
    }
}

public sealed class UserSkipService
{
    private readonly IUserSkipStore store;

    public UserSkipService(IUserSkipStore store)
    {
        this.store = store;
    }

    public UserSkipAdditions Load() => store.Load();

    public UserSkipAddStatus TryAdd(UserSkipKind kind, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return UserSkipAddStatus.Empty;
        }

        var trimmed = raw.Trim();
        if (trimmed.Contains('/') || trimmed.Contains('\\'))
        {
            return UserSkipAddStatus.Empty;
        }

        var current = store.Load();
        if (kind == UserSkipKind.Extension)
        {
            var value = trimmed;
            if (!value.StartsWith('.'))
            {
                value = "." + value;
            }

            value = value.ToLowerInvariant();
            if (value == ".")
            {
                return UserSkipAddStatus.Empty;
            }

            var withoutDot = value[1..];
            if (BuiltInSkips.ContainsCodeExtension(value)
                || BuiltInSkips.ContainsSecretExtension(withoutDot)
                || BuiltInSkips.ContainsDatabaseExtension(withoutDot))
            {
                return UserSkipAddStatus.BuiltIn;
            }

            if (current.Extensions.Any(candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)))
            {
                return UserSkipAddStatus.Duplicate;
            }

            var next = new UserSkipAdditions(
                [.. current.Extensions, value],
                current.FolderNames,
                current.FileNames);
            store.Save(next);
            return UserSkipAddStatus.Added;
        }

        if (kind == UserSkipKind.FolderName)
        {
            var value = trimmed;
            if (BuiltInSkips.ContainsFolder(value))
            {
                return UserSkipAddStatus.BuiltIn;
            }

            if (current.FolderNames.Any(candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)))
            {
                return UserSkipAddStatus.Duplicate;
            }

            var next = new UserSkipAdditions(
                current.Extensions,
                [.. current.FolderNames, value],
                current.FileNames);
            store.Save(next);
            return UserSkipAddStatus.Added;
        }

        var fileValue = trimmed;
        if (BuiltInSkips.ContainsExactCodeName(fileValue)
            || BuiltInSkips.ContainsSecretName(fileValue)
            || BuiltInSkips.ContainsLockName(fileValue)
            || BuiltInSkips.SecretDisplayRows.Any(candidate => string.Equals(candidate, fileValue, StringComparison.OrdinalIgnoreCase)))
        {
            return UserSkipAddStatus.BuiltIn;
        }

        if (current.FileNames.Any(candidate => string.Equals(candidate, fileValue, StringComparison.OrdinalIgnoreCase)))
        {
            return UserSkipAddStatus.Duplicate;
        }

        var added = new UserSkipAdditions(
            current.Extensions,
            current.FolderNames,
            [.. current.FileNames, fileValue]);
        store.Save(added);
        return UserSkipAddStatus.Added;
    }

    public bool TryRemove(UserSkipKind kind, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        if (trimmed.Contains('/') || trimmed.Contains('\\'))
        {
            return false;
        }

        var current = store.Load();
        if (kind == UserSkipKind.Extension)
        {
            var value = trimmed;
            if (!value.StartsWith('.'))
            {
                value = "." + value;
            }

            value = value.ToLowerInvariant();
            if (value == ".")
            {
                return false;
            }

            var withoutDot = value[1..];
            if (BuiltInSkips.ContainsCodeExtension(value)
                || BuiltInSkips.ContainsSecretExtension(withoutDot)
                || BuiltInSkips.ContainsDatabaseExtension(withoutDot))
            {
                return false;
            }

            var found = current.Extensions.FirstOrDefault(candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase));
            if (found is null)
            {
                return false;
            }

            var next = new UserSkipAdditions(
                current.Extensions.Where(candidate => !string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)).ToList(),
                current.FolderNames,
                current.FileNames);
            store.Save(next);
            return true;
        }

        if (kind == UserSkipKind.FolderName)
        {
            var value = trimmed;
            if (BuiltInSkips.ContainsFolder(value))
            {
                return false;
            }

            if (!current.FolderNames.Any(candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var next = new UserSkipAdditions(
                current.Extensions,
                current.FolderNames.Where(candidate => !string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)).ToList(),
                current.FileNames);
            store.Save(next);
            return true;
        }

        var fileValue = trimmed;
        if (BuiltInSkips.ContainsExactCodeName(fileValue)
            || BuiltInSkips.ContainsSecretName(fileValue)
            || BuiltInSkips.ContainsLockName(fileValue)
            || BuiltInSkips.SecretDisplayRows.Any(candidate => string.Equals(candidate, fileValue, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!current.FileNames.Any(candidate => string.Equals(candidate, fileValue, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var removed = new UserSkipAdditions(
            current.Extensions,
            current.FolderNames,
            current.FileNames.Where(candidate => !string.Equals(candidate, fileValue, StringComparison.OrdinalIgnoreCase)).ToList());
        store.Save(removed);
        return true;
    }

    public static string? MessageKey(UserSkipAddStatus status)
    {
        if (status == UserSkipAddStatus.Added)
        {
            return null;
        }

        if (status == UserSkipAddStatus.Empty)
        {
            return "Skips_Empty";
        }

        if (status == UserSkipAddStatus.Duplicate)
        {
            return "Skips_Duplicate";
        }

        if (status == UserSkipAddStatus.BuiltIn)
        {
            return "Skips_BuiltIn";
        }

        return "Skips_Empty";
    }
}
