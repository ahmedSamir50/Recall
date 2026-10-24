using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Scope;
using Recall.Core.Text;
using System.Text;

namespace Recall.Core.Search;

public static class Bm25OnlyRank
{
    public static int Count(string rawText, string query)
    {
        ArgumentNullException.ThrowIfNull(rawText);
        ArgumentNullException.ThrowIfNull(query);
        var rawPieces = Pieces(rawText);
        var queryPieces = Pieces(query);
        var used = new bool[rawPieces.Count];
        var count = 0;
        foreach (var piece in queryPieces)
        {
            for (var i = 0; i < rawPieces.Count; i++)
            {
                if (used[i])
                {
                    continue;
                }

                if (string.Equals(rawPieces[i], piece, StringComparison.Ordinal))
                {
                    used[i] = true;
                    count++;
                    break;
                }
            }
        }

        return count;
    }

    public static IReadOnlyList<TextSearchHit> Merge(
        IReadOnlyList<TextSearchHit> ftsHits,
        IReadOnlyList<Bm25OnlyRow> rows,
        string query,
        IReadOnlyList<ScopeFolderGrant> grants)
    {
        ArgumentNullException.ThrowIfNull(ftsHits);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(grants);
        var merged = new List<TextSearchHit>(ftsHits);
        var appended = new List<TextSearchHit>();
        foreach (var row in rows)
        {
            var overlap = Count(row.RawText, query);
            if (overlap == 0)
            {
                continue;
            }

            var enumerated = new HashSet<string>(StringComparer.Ordinal) { row.Path };
            var grantPath = GrantMembership.GrantPathFor(row.Path, grants, enumerated);
            if (grantPath is null)
            {
                continue;
            }

            appended.Add(new TextSearchHit(
                row.ContentId,
                row.Path,
                Path.GetFileName(row.Path),
                DateTimeOffset.UnixEpoch,
                TextMatchReason.DenseAndFullText,
                overlap,
                RawNote: row.RawText,
                GrantPath: grantPath));
        }

        appended.Sort(static (left, right) =>
        {
            var byScore = right.Score.CompareTo(left.Score);
            if (byScore != 0)
            {
                return byScore;
            }

            var byId = string.Compare(left.ContentId, right.ContentId, StringComparison.Ordinal);
            if (byId != 0)
            {
                return byId;
            }

            return string.Compare(left.SourcePath, right.SourcePath, StringComparison.Ordinal);
        });
        merged.AddRange(appended);
        return merged.Take(RecallIndexCaps.RawListCap).ToList();
    }

    private static List<string> Pieces(string text)
    {
        var folded = KeywordFold.Fold(text);
        var tokens = KeywordFold.Tokens(folded);
        if (tokens.Count > 0)
        {
            return tokens.ToList();
        }

        var pieces = new List<string>();
        var builder = new StringBuilder();
        foreach (var rune in folded.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                if (builder.Length > 0)
                {
                    pieces.Add(builder.ToString());
                    builder.Clear();
                }

                continue;
            }

            builder.Append(rune);
        }

        if (builder.Length > 0)
        {
            pieces.Add(builder.ToString());
        }

        return pieces;
    }
}
