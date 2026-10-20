namespace Recall.Core.Search;

public static class TextSearchFilter
{
    public const string PathLiveField = "path_live";
    public const string EncoderHashField = "encoder_hash";
    public const string GrantPathField = "grant_path";

    public static string Build(IReadOnlySet<string> grantPaths, string encoderHash)
    {
        ArgumentNullException.ThrowIfNull(grantPaths);
        ArgumentNullException.ThrowIfNull(encoderHash);
        if (grantPaths.Count == 0)
        {
            return string.Empty;
        }

        var grants = grantPaths.OrderBy(p => p, StringComparer.Ordinal).ToList();
        var inList = string.Join(", ", grants.Select(p => "\"" + Escape(p) + "\""));
        return $"{PathLiveField} = 1 AND {EncoderHashField} = \"{Escape(encoderHash)}\" AND {GrantPathField} IN ({inList})";
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
