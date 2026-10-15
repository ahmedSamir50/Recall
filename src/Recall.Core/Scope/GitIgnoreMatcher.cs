using Recall.Core.Scope;

namespace Recall.Core.Scope;

public sealed record GitIgnoreFile(string DirectoryRelative, IReadOnlyList<string> Lines);

public readonly record struct GitIgnorePattern(string Body, bool Negated, bool DirectoryOnly, bool Anchored);

public enum GitIgnoreDecision
{
    None = 0,
    Ignore = 1,
    Include = 2,
}

public static class GitIgnoreMatcher
{
    public static bool TryParse(string line, out GitIgnorePattern pattern)
    {
        pattern = default;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (line.StartsWith("\\#", StringComparison.Ordinal) || line.StartsWith("\\!", StringComparison.Ordinal))
        {
            var body = line[1..];
            if (string.IsNullOrEmpty(body))
            {
                return false;
            }

            var directoryOnly = false;
            if (body.EndsWith("/", StringComparison.Ordinal) && body.Length > 0)
            {
                directoryOnly = true;
                body = body[..^1];
                if (string.IsNullOrEmpty(body))
                {
                    return false;
                }
            }

            var anchored = body.StartsWith("/", StringComparison.Ordinal) || body.Contains('/', StringComparison.Ordinal);
            if (body.StartsWith("/", StringComparison.Ordinal))
            {
                body = body[1..];
                if (string.IsNullOrEmpty(body))
                {
                    return false;
                }
            }

            pattern = new GitIgnorePattern(body, false, directoryOnly, anchored);
            return true;
        }

        if (line[0] == '#')
        {
            return false;
        }

        var negated = false;
        var rest = line;
        if (rest.StartsWith("!", StringComparison.Ordinal))
        {
            negated = true;
            rest = rest[1..];
            if (string.IsNullOrEmpty(rest))
            {
                return false;
            }
        }

        var dirOnly = false;
        if (rest.EndsWith("/", StringComparison.Ordinal))
        {
            dirOnly = true;
            rest = rest[..^1];
            if (string.IsNullOrEmpty(rest))
            {
                return false;
            }
        }

        var isAnchored = rest.StartsWith("/", StringComparison.Ordinal) || rest.Contains('/', StringComparison.Ordinal);
        if (rest.StartsWith("/", StringComparison.Ordinal))
        {
            rest = rest[1..];
            if (string.IsNullOrEmpty(rest))
            {
                return false;
            }
        }

        pattern = new GitIgnorePattern(rest, negated, dirOnly, isAnchored);
        return true;
    }

    public static GitIgnoreDecision LastDecision(string relativePath, bool isDirectory, IReadOnlyList<GitIgnoreFile> files)
    {
        if (string.IsNullOrEmpty(relativePath) || files is null || files.Count == 0)
        {
            return GitIgnoreDecision.None;
        }

        var candidate = NormalizeRelative(relativePath);
        if (string.IsNullOrEmpty(candidate))
        {
            return GitIgnoreDecision.None;
        }

        var decision = GitIgnoreDecision.None;
        foreach (var file in files)
        {
            var dir = NormalizeRelative(file.DirectoryRelative ?? string.Empty);
            string sub;
            if (string.IsNullOrEmpty(dir))
            {
                sub = candidate;
            }
            else if (candidate.Equals(dir, StringComparison.Ordinal))
            {
                continue;
            }
            else if (candidate.StartsWith(dir + "/", StringComparison.Ordinal))
            {
                sub = candidate[(dir.Length + 1)..];
            }
            else
            {
                continue;
            }

            if (file.Lines is null)
            {
                continue;
            }

            foreach (var line in file.Lines)
            {
                if (!TryParse(line, out var pattern))
                {
                    continue;
                }

                if (pattern.DirectoryOnly && !isDirectory)
                {
                    continue;
                }

                var matched = false;
                if (pattern.Anchored)
                {
                    matched = GlobMatch(pattern.Body, sub);
                }
                else
                {
                    matched = GlobMatch(pattern.Body, sub);
                    if (!matched)
                    {
                        var slash = sub.LastIndexOf('/');
                        var baseName = slash >= 0 ? sub[(slash + 1)..] : sub;
                        matched = GlobMatch(pattern.Body, baseName);
                    }
                }

                if (matched)
                {
                    decision = pattern.Negated ? GitIgnoreDecision.Include : GitIgnoreDecision.Ignore;
                }
            }
        }

        return decision;
    }

    public static bool IsIgnored(string relativePath, IReadOnlyList<GitIgnoreFile> files)
    {
        if (string.IsNullOrEmpty(relativePath) || files is null || files.Count == 0)
        {
            return false;
        }

        var candidate = NormalizeRelative(relativePath);
        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        var segments = candidate.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 1; i < segments.Length; i++)
        {
            var prefix = string.Join("/", segments, 0, i);
            if (LastDecision(prefix, true, files) == GitIgnoreDecision.Ignore)
            {
                return true;
            }
        }

        return LastDecision(candidate, false, files) == GitIgnoreDecision.Ignore;
    }

    public static IReadOnlyList<GitIgnoreFile> LoadDirectoryFiles(string grantRoot, string directory)
    {
        if (string.IsNullOrWhiteSpace(grantRoot) || string.IsNullOrWhiteSpace(directory))
        {
            return [];
        }

        if (!ScopeGrantPolicy.IsInsideGrant(grantRoot, directory))
        {
            return [];
        }

        var grantNorm = ScopeGrantPolicy.Normalize(grantRoot);
        var dirNorm = ScopeGrantPolicy.Normalize(directory);
        string[] segments;
        if (grantNorm.Equals(dirNorm, StringComparison.OrdinalIgnoreCase))
        {
            segments = [];
        }
        else if (ScopeGrantPolicy.IsChildPath(grantNorm, dirNorm))
        {
            var suffix = dirNorm[grantNorm.Length..].TrimStart('\\', '/');
            segments = suffix.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            return [];
        }

        var result = new List<GitIgnoreFile>();
        for (var depth = 0; depth <= segments.Length; depth++)
        {
            var current = grantNorm;
            for (var j = 0; j < depth; j++)
            {
                current = Path.Combine(current, segments[j]);
            }

            var relative = depth == 0 ? string.Empty : string.Join("/", segments, 0, depth);
            var ignorePath = Path.Combine(current, ".gitignore");
            if (File.Exists(ignorePath))
            {
                var lines = File.ReadAllLines(ignorePath);
                result.Add(new GitIgnoreFile(relative, lines));
            }

            var excludePath = Path.Combine(current, ".git", "info", "exclude");
            if (File.Exists(excludePath))
            {
                var lines = File.ReadAllLines(excludePath);
                result.Add(new GitIgnoreFile(relative, lines));
            }
        }

        return result;
    }

    private static string NormalizeRelative(string path)
    {
        var fixedSlashes = path.Replace('\\', '/');
        var trimmed = fixedSlashes.Trim();
        trimmed = trimmed.TrimStart('/');
        trimmed = trimmed.TrimEnd('/');
        while (trimmed.Contains("//", StringComparison.Ordinal))
        {
            trimmed = trimmed.Replace("//", "/", StringComparison.Ordinal);
        }

        return trimmed;
    }

    private static bool GlobMatch(string pattern, string text)
    {
        var regex = "^" + GlobToRegex(pattern) + "$";
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, regex, System.Text.RegularExpressions.RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static string GlobToRegex(string pattern)
    {
        var builder = new System.Text.StringBuilder();
        var i = 0;
        while (i < pattern.Length)
        {
            var c = pattern[i];
            if (c == '*')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                {
                    var after = i + 2 < pattern.Length ? pattern[i + 2] : '\0';
                    var beforeSlash = i > 0 && pattern[i - 1] == '/';
                    if (after == '/')
                    {
                        builder.Append("(.*/)?");
                        i += 3;
                        continue;
                    }

                    if (beforeSlash && (i + 2 >= pattern.Length))
                    {
                        builder.Append("(/.*)?");
                        i += 2;
                        continue;
                    }

                    builder.Append(".*");
                    i += 2;
                    while (i < pattern.Length && pattern[i] == '*')
                    {
                        i++;
                    }

                    continue;
                }

                builder.Append("[^/]*");
                i++;
                continue;
            }

            if (c == '?')
            {
                builder.Append("[^/]");
                i++;
                continue;
            }

            if (c == '[')
            {
                var close = pattern.IndexOf(']', i + 1);
                if (close < 0)
                {
                    builder.Append("\\[");
                    i++;
                    continue;
                }

                var content = pattern.Substring(i + 1, close - i - 1);
                if (content.Length == 0)
                {
                    builder.Append("\\[\\]");
                    i = close + 1;
                    continue;
                }

                if (content[0] == '!')
                {
                    content = "^" + content[1..];
                }

                content = content.Replace("\\", "\\\\", StringComparison.Ordinal);
                builder.Append("(?!/)");
                builder.Append('[');
                builder.Append(content);
                builder.Append(']');
                i = close + 1;
                continue;
            }

            if (c == '\\')
            {
                builder.Append("\\\\");
                i++;
                continue;
            }

            if ("+()^$.{}|".IndexOf(c, StringComparison.Ordinal) >= 0)
            {
                builder.Append('\\');
                builder.Append(c);
            }
            else
            {
                builder.Append(c);
            }

            i++;
        }

        return builder.ToString();
    }
}
