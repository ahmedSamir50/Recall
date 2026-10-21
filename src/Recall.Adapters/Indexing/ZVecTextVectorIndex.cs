using System.Globalization;
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Search;
using ZVec.NET;

namespace Recall.Adapters.Indexing;

public sealed class ZVecTextVectorIndex : ITextVectorIndex, IDisposable
{
    public const string CollectionName = "items_text";
    public const string EmbeddingField = "embedding";
    public const string FtsKeyField = "fts_key";
    public const string RawTextField = "raw_text";
    public const string SourcePathField = "source_path";
    public const string FileNameField = "file_name";
    public const string ModifiedField = "modified_ms";
    public const string PathLiveField = "path_live";
    public const string EncoderHashField = "encoder_hash";
    public const string GrantPathField = "grant_path";
    public const string OrdinalField = "ordinal";

    private readonly string rootPath;
    private readonly IIndexPolicy indexPolicy;
    private readonly ZVecFactory factory;
    private readonly IZvecCollection collection;
    private readonly HashSet<string> knownIds = new(StringComparer.Ordinal);

    public ZVecTextVectorIndex(string databaseRoot, IIndexPolicy? indexPolicy = null, IIngestCursorStore? cursors = null)
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
                new ZVecFieldSchema { Name = PathLiveField, DataType = ZVecDataType.Int32 },
                new ZVecFieldSchema { Name = EncoderHashField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = GrantPathField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = OrdinalField, DataType = ZVecDataType.Int32 },
                new ZVecFieldSchema { Name = RawTextField, DataType = ZVecDataType.String },
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
                    Name = FtsKeyField,
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

        IZvecCollection opened;
        // A missing directory is Create before the flag is read. Passing true makes an existing directory Open so its schema can be checked.
        if (TextIndexOpenPolicy.Decide(Directory.Exists(rootPath), hasFtsKeyVector: true) == TextIndexOpenKind.Create)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(rootPath)!);
            opened = factory.CreateAndOpen(rootPath, schema);
        }
        else
        {
            opened = factory.Open(rootPath);
            if (TextIndexOpenPolicy.Decide(true, SchemaHasFtsKey(opened.Schema)) == TextIndexOpenKind.Replace)
            {
                opened.Destroy();
                if (Directory.Exists(rootPath))
                {
                    Directory.Delete(rootPath, recursive: true);
                }

                cursors?.ResetTextIngest();
                Directory.CreateDirectory(Path.GetDirectoryName(rootPath)!);
                opened = factory.CreateAndOpen(rootPath, schema);
            }
        }

        collection = opened;
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
            sparseVectors: null,
            fields: new Dictionary<string, object>
            {
                [SourcePathField] = insert.SourcePath,
                [FileNameField] = insert.FileName,
                [FtsKeyField] = insert.FoldedKey,
                [RawTextField] = insert.RawNoteText,
                [ModifiedField] = insert.ModifiedAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                [PathLiveField] = insert.PathLive ? 1 : 0,
                [EncoderHashField] = insert.EncoderHash,
                [GrantPathField] = insert.GrantPath,
                [OrdinalField] = insert.Ordinal,
            });

        var result = collection.Insert(doc);
        if (!result.IsSuccess)
        {
            return false;
        }

        knownIds.Add(insert.ContentId);
        return true;
    }

    public bool TryDelete(string contentId)
    {
        if (collection.Fetch(contentId, includeVector: false) is null)
        {
            return false;
        }

        var result = collection.Delete(contentId);
        if (!result.IsSuccess)
        {
            return false;
        }

        knownIds.Remove(contentId);
        return true;
    }

    public void Reconcile(IReadOnlyList<VectorLiveState> states)
    {
        foreach (var state in states)
        {
            var existing = collection.Fetch(state.ContentId, includeVector: false);
            if (existing is null)
            {
                continue;
            }

            var fields = new Dictionary<string, object>(existing.Fields)
            {
                [PathLiveField] = state.PathLive ? 1 : 0,
                [EncoderHashField] = state.EncoderHash,
                [GrantPathField] = state.GrantPath,
            };
            var updated = ZVecDoc.Create(existing.Id, existing.DenseVectors, existing.SparseVectors, fields);
            collection.Update(updated);
        }
    }

    public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rankConstant = HybridFusion.RrfRankConstant;
        TextVectorIndexGuard.EnsureHybridQueryUsesRrf(rankConstant, indexPolicy);
        var topK = request.TopK;
        var filter = string.IsNullOrWhiteSpace(request.Filter) ? null : request.Filter;
        if (request.UseInDatabaseRrf)
        {
            var dense = new ZVecQuery
            {
                FieldName = EmbeddingField,
                Vector = request.QueryVector512,
                QueryParams = new ZVecHnswQueryParams { EfSearch = RecallIndexPolicy.TextHnswEfSearch },
            };
            var fts = new ZVecQuery
            {
                FieldName = FtsKeyField,
                Fts = new ZVecFtsQuery { QueryString = request.Query, DefaultOperator = ZVecFtsDefaultOperator.And },
            };
            var reranker = new ZVecRrfReranker { TopN = topK, RankConstant = rankConstant };
            return MapDocsStatic(collection.Query([dense, fts], topk: topK, reranker: reranker, filter: filter, includeVector: false));
        }

        if (request.QueryVector512.Length > 0)
        {
            var dense = new ZVecQuery
            {
                FieldName = EmbeddingField,
                Vector = request.QueryVector512,
                QueryParams = new ZVecHnswQueryParams { EfSearch = RecallIndexPolicy.TextHnswEfSearch },
            };
            return MapDocsStatic(collection.Query(dense, topk: topK, filter: filter, includeVector: false));
        }

        var ftsOnly = new ZVecQuery
        {
            FieldName = FtsKeyField,
            Fts = new ZVecFtsQuery { QueryString = request.Query, DefaultOperator = ZVecFtsDefaultOperator.And },
        };
            return MapDocsStatic(collection.Query(ftsOnly, topk: topK, filter: filter, includeVector: false));
    }

    private static List<TextSearchHit> MapDocsStatic(IReadOnlyList<ZVecDoc> docs)
    {
        var hits = new List<TextSearchHit>();
        foreach (var doc in docs)
        {
            hits.Add(new TextSearchHit(
                doc.Id,
                ReadField(doc, SourcePathField),
                ReadField(doc, FileNameField),
                ReadModified(doc),
                TextMatchReason.DenseAndFullText,
                doc.Score,
                ReadInt(doc, OrdinalField),
                ReadField(doc, RawTextField),
                ReadField(doc, GrantPathField)));
        }

        return hits;
    }

    private static bool SchemaHasFtsKey(ZVecCollectionSchema? schema)
    {
        if (schema is null)
        {
            return false;
        }

        return schema.Vectors.Any(v => string.Equals(v.Name, FtsKeyField, StringComparison.Ordinal));
    }

    private static string ReadField(ZVecDoc doc, string field) =>
        doc.Fields.TryGetValue(field, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

    private static int ReadInt(ZVecDoc doc, string field)
    {
        if (doc.Fields.TryGetValue(field, out var value) && int.TryParse(value?.ToString(), out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static DateTimeOffset ReadModified(ZVecDoc doc)
    {
        if (doc.Fields.TryGetValue(ModifiedField, out var value)
            && long.TryParse(value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }

        return DateTimeOffset.UnixEpoch;
    }

    public void Dispose()
    {
        collection.Dispose();
        factory.Shutdown();
        factory.Dispose();
    }
}
