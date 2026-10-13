using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.Core.Vectors;

namespace Recall.Core.Search;

public sealed class TextSearchService : ITextSearchService
{
    private readonly ITextEmbeddingEncoder encoder;
    private readonly ITextVectorIndex index;
    private readonly IIndexPolicy indexPolicy;

    public TextSearchService(ITextEmbeddingEncoder encoder, ITextVectorIndex index, IIndexPolicy indexPolicy)
    {
        this.encoder = encoder;
        this.index = index;
        this.indexPolicy = indexPolicy;
    }

    public IReadOnlyList<TextSearchHit> Find(
        string query,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var grantedPaths = BuildGrantedPathSet(grants);
        if (grantedPaths.Count == 0)
        {
            return [];
        }

        var prefixed = EmbeddingTaskPrefixes.WrapSearchQuery(query);
        var full768 = encoder.EmbedPrefixedText(prefixed, cancellationToken).ToArray();
        MatryoshkaVectorMath.L2Normalize(full768);
        var storedWidth = (int)indexPolicy.TextStoredWidth;
        var query512 = MatryoshkaVectorMath.CutAndNormalize(full768, storedWidth);

        var request = new TextSearchRequest(query, query512, grantedPaths, TopK: 20);
        return index.Search(request, cancellationToken);
    }

    private static HashSet<string> BuildGrantedPathSet(IReadOnlyList<ScopeFolderGrant> grants)
    {
        return grants
            .Where(grant => grant.EnabledKinds.HasFlag(GrantFileKinds.Text))
            .Select(grant => grant.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
