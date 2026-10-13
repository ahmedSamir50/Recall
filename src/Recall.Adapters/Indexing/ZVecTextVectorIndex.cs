using System.Globalization;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using ZVec.NET;
using ZVec.NET.Query;

namespace Recall.Adapters.Indexing;

public sealed class ZVecTextVectorIndex : ITextVectorIndex, IDisposable
{
    public const string CollectionName = "items_text";
    public const string EmbeddingField = "embedding";
    public const string RawTextField = "raw_text";
    public const string SourcePathField = "source_path";
    public const string FileNameField = "file_name";
    public const string ModifiedField = "modified_ms";

    private readonly string rootPath;
    private readonly IIndexPolicy indexPolicy;
    private readonly ZVecFactory factory;
    private readonly IZvecCollection collection;
    private readonly HashSet<string> knownIds = new(StringComparer.Ordinal);

    public ZVecTextVectorIndex(string databaseRoot, IIndexPolicy? indexPolicy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseRoot);
        this.indexPolicy = indexPolicy ?? new RecallIndexPolicy();
        rootPath = Path.Combine(databaseRoot, CollectionName);
        factory = new ZVecFactory();
        factory.Initialize();
        var schema = new ZVecCollectionSchema
        {
            Name = CollectionName,
            Fields =
            [
                new ZVecFieldSchema { Name = SourcePathField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = FileNameField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = ModifiedField, DataType = ZVecDataType.String },
            ],
            Vectors =
            [
                new ZVecVectorSchema
                {
                    Name = EmbeddingField,
                    DataType = ZVecDataType.VectorFp32,
                    Dimension = 512,
                    IndexParam = new ZVecHnswIndexParam
                    {
                        MetricType = ZVecMetricType.Cosine,
                        M = RecallIndexPolicy.TextHnswM,
                        EfConstruction = RecallIndexPolicy.TextHnswEfConstruction,
                        QuantizeType = ZVecQuantizeType.Undefined,
                    },
                },
                new ZVecVectorSchema
                {
                    Name = RawTextField,
                    DataType = ZVecDataType.String,
                    Dimension = 0,
                    IndexParam = new ZVecFtsIndexParam
                    {
                        Tokenizer = ZVecFtsTokenizer.Standard,
                        Filters = [ZVecFtsTokenFilter.Lowercase],
                    },
                },
            ],
        };

        if (Directory.Exists(rootPath))
        {
            collection = factory.Open(rootPath);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(rootPath)!);
            collection = factory.CreateAndOpen(rootPath, schema);
        }
    }

    public bool TryInsert(TextIndexInsert insert)
    {
        if (knownIds.Contains(insert.ContentId))
        {
            return false;
        }

        if (collection.Fetch(insert.ContentId, includeVector: false) is not null)
        {
            knownIds.Add(insert.ContentId);
            return false;
        }

        var doc = ZVecDoc.Create(
            insert.ContentId,
            denseVectors: new Dictionary<string, ReadOnlyMemory<float>>
            {
                [EmbeddingField] = insert.Vector512,
            },
            fields: new Dictionary<string, object>
            {
                [SourcePathField] = insert.SourcePath,
                [FileNameField] = insert.FileName,
                [RawTextField] = insert.RawNoteText,
                [ModifiedField] = insert.ModifiedAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            });

        var result = collection.Insert(doc);
        if (!result.IsSuccess)
        {
            return false;
        }

        knownIds.Add(insert.ContentId);
        return true;
    }

    public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rankConstant = HybridFusion.RrfRankConstant;
        TextVectorIndexGuard.EnsureHybridQueryUsesRrf(rankConstant, indexPolicy);
        var dense = new ZVecQuery
        {
            FieldName = EmbeddingField,
            Vector = request.QueryVector512,
            QueryParams = new ZVecHnswQueryParams { EfSearch = RecallIndexPolicy.TextHnswEfSearch },
        };
        var fts = new ZVecQuery
        {
            FieldName = RawTextField,
            Fts = new ZVecFtsQuery { QueryString = request.Query, DefaultOperator = ZVecFtsDefaultOperator.And },
        };

        var reranker = new ZVecRrfReranker { TopN = request.TopK, RankConstant = rankConstant };
        var docs = collection.Query([dense, fts], topk: request.TopK, reranker: reranker, filter: (string?)null, includeVector: false);

        var hits = new List<TextSearchHit>();
        foreach (var doc in docs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = ReadField(doc, SourcePathField);
            if (!IsUnderGrant(path, request.GrantedPaths))
            {
                continue;
            }

            hits.Add(new TextSearchHit(
                doc.Id,
                path,
                ReadField(doc, FileNameField),
                ReadModified(doc),
                TextMatchReason.DenseAndFullText,
                doc.Score));
        }

        return hits;
    }

    private static string ReadField(ZVecDoc doc, string field) =>
        doc.Fields.TryGetValue(field, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

    private static DateTimeOffset ReadModified(ZVecDoc doc)
    {
        if (doc.Fields.TryGetValue(ModifiedField, out var value)
            && long.TryParse(value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }

        return DateTimeOffset.UnixEpoch;
    }

    private static bool IsUnderGrant(string path, IReadOnlySet<string> grantedPaths)
    {
        return grantedPaths.Any(grant => ScopeGrantPolicy.IsInsideGrant(grant, path));
    }

    public void Dispose()
    {
        collection.Dispose();
        factory.Shutdown();
        factory.Dispose();
    }
}
