using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Search;
using Recall.Core.Vectors;
using System.Globalization;
using ZVec.NET;

namespace Recall.Adapters.Indexing;

public sealed class ZVecMediaVectorIndex : IMediaVectorIndex, IDisposable
{
    public const string EmbeddingField = "embedding";
    public const string WordsField = "words";
    public const string SourcePathField = "source_path";
    public const string FileNameField = "file_name";
    public const string PathLiveField = "path_live";
    public const string EncoderHashField = "encoder_hash";
    public const string GrantPathField = "grant_path";
    public const string StartSecondsField = "start_seconds";
    public const string EndSecondsField = "end_seconds";

    private readonly string databaseRoot;
    private readonly ZVecFactory factory;
    private readonly Dictionary<string, IZvecCollection> collections = new(StringComparer.Ordinal);
    private readonly object sync = new();
    private bool disposed;

    public ZVecMediaVectorIndex(string databaseRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseRoot);
        this.databaseRoot = databaseRoot;
        factory = new ZVecFactory();
        factory.Initialize();
    }

    public void Create(string collectionName, int width)
    {
        lock (sync)
        {
            CreateCore(collectionName, width);
        }
    }

    private void CreateCore(string collectionName, int width)
    {
        if (!MediaCollections.IsMedia(collectionName))
        {
            throw new ArgumentException("Only picture, video, and audio collections are media collections.", nameof(collectionName));
        }

        var expected = MediaCollections.WidthFor(collectionName);
        if (width != expected)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (collections.ContainsKey(collectionName))
        {
            return;
        }

        var rootPath = Path.Combine(databaseRoot, collectionName);
        if (Directory.Exists(rootPath))
        {
            collections[collectionName] = factory.Open(rootPath);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(rootPath)!);
        collections[collectionName] = factory.CreateAndOpen(rootPath, BuildSchema(collectionName, width));
    }

    internal static ZVecCollectionSchema BuildSchema(string collectionName, int width)
    {
        var schema = new ZVecCollectionSchema
        {
            Name = collectionName,
            Fields =
            [
                new ZVecFieldSchema { Name = SourcePathField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = FileNameField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = PathLiveField, DataType = ZVecDataType.Int32 },
                new ZVecFieldSchema { Name = EncoderHashField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = GrantPathField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = StartSecondsField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = EndSecondsField, DataType = ZVecDataType.String },
            ],
            Vectors =
            [
                new ZVecVectorSchema
                {
                    Name = EmbeddingField,
                    DataType = ZVecDataType.VectorFp32,
                    Dimension = width,
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
                    Name = WordsField,
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

        return schema;
    }

    public bool TryInsert(string collectionName, MediaIndexInsert insert)
    {
        lock (sync)
        {
            return TryInsertCore(collectionName, insert);
        }
    }

    private bool TryInsertCore(string collectionName, MediaIndexInsert insert)
    {
        var collection = RequireCollection(collectionName);
        var expectedWidth = MediaCollections.WidthFor(collectionName);
        if (insert.Vector768.Length != expectedWidth)
        {
            throw new ArgumentException("Vector length must match the collection width.");
        }

        if (collection.Fetch(insert.ContentId, includeVector: false) is not null)
        {
            return false;
        }

        var doc = ZVecDoc.Create(
            insert.ContentId,
            denseVectors: new Dictionary<string, ReadOnlyMemory<float>>
            {
                [EmbeddingField] = insert.Vector768,
            },
            sparseVectors: null,
            fields: new Dictionary<string, object>
            {
                [WordsField] = insert.Words,
                [SourcePathField] = insert.SourcePath,
                [FileNameField] = insert.FileName,
                [PathLiveField] = insert.PathLive ? 1 : 0,
                [EncoderHashField] = insert.EncoderHash,
                [GrantPathField] = insert.GrantPath,
                [StartSecondsField] = insert.StartSeconds,
                [EndSecondsField] = insert.EndSeconds,
            });

        return collection.Insert(doc).IsSuccess;
    }

    public bool TryRead(string contentId, out MediaStoredVector vector)
    {
        lock (sync)
        {
            return TryReadCore(contentId, out vector);
        }
    }

    private bool TryReadCore(string contentId, out MediaStoredVector vector)
    {
        OpenExistingCollections();
        foreach (var pair in collections)
        {
            var doc = pair.Value.Fetch(contentId, includeVector: true);
            if (doc is null)
            {
                continue;
            }

            var embedding = doc.DenseVectors.TryGetValue(EmbeddingField, out var dense)
                ? dense.ToArray()
                : [];
            vector = new MediaStoredVector(
                contentId,
                pair.Key,
                embedding,
                ReadField(doc, WordsField),
                ReadField(doc, EncoderHashField),
                ReadField(doc, GrantPathField),
                ReadField(doc, SourcePathField),
                ReadField(doc, StartSecondsField),
                ReadField(doc, EndSecondsField),
                ReadField(doc, PathLiveField) == "1");
            return true;
        }

        vector = null!;
        return false;
    }

    public bool TryDelete(string contentId)
    {
        lock (sync)
        {
            return TryDeleteCore(contentId);
        }
    }

    private bool TryDeleteCore(string contentId)
    {
        OpenExistingCollections();
        var deleted = false;
        foreach (var collection in collections.Values)
        {
            if (collection.Fetch(contentId, includeVector: false) is null)
            {
                continue;
            }

            if (collection.Delete(contentId).IsSuccess)
            {
                deleted = true;
            }
        }

        return deleted;
    }

    public void Reconcile(IReadOnlyList<VectorLiveState> states)
    {
        lock (sync)
        {
            ReconcileCore(states);
        }
    }

    private void ReconcileCore(IReadOnlyList<VectorLiveState> states)
    {
        ArgumentNullException.ThrowIfNull(states);
        foreach (var state in states)
        {
            foreach (var collection in collections.Values)
            {
                var existing = collection.Fetch(state.ContentId, includeVector: false);
                if (existing is null)
                {
                    continue;
                }

                var fields = new Dictionary<string, object>(existing.Fields)
                {
                    [PathLiveField] = state.PathLive ? 1 : 0,
                };
                collection.Update(ZVecDoc.Create(existing.Id, existing.DenseVectors, existing.SparseVectors, fields));
            }
        }
    }

    public IReadOnlyList<MediaSearchHit> Search(MediaSearchRequest request, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return SearchCore(request, cancellationToken);
        }
    }

    private IReadOnlyList<MediaSearchHit> SearchCore(MediaSearchRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryOpenCollection(request.CollectionName, out var collection) || collection is null)
        {
            return [];
        }
        var dense = new ZVecQuery
        {
            FieldName = EmbeddingField,
            Vector = request.QueryVector768,
            QueryParams = new ZVecHnswQueryParams { EfSearch = RecallIndexPolicy.TextHnswEfSearch },
        };
        var filter = string.IsNullOrWhiteSpace(request.Filter) ? null : request.Filter;
        var docs = collection.Query(dense, topk: request.TopK, filter: filter, includeVector: false);
        var hits = new List<MediaSearchHit>(docs.Count);
        foreach (var doc in docs)
        {
            hits.Add(new MediaSearchHit(
                doc.Id,
                ReadField(doc, SourcePathField),
                ReadSeconds(doc, StartSecondsField),
                ReadSeconds(doc, EndSecondsField),
                doc.Score));
        }

        return hits;
    }

    public IReadOnlyList<MediaSearchHit> SearchWords(string collectionName, string query, string filter, int topK, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        lock (sync)
        {
            return SearchWordsCore(collectionName, query, filter, topK, cancellationToken);
        }
    }

    private IReadOnlyList<MediaSearchHit> SearchWordsCore(string collectionName, string query, string filter, int topK, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryOpenCollection(collectionName, out var collection) || collection is null)
        {
            return [];
        }
        var fts = new ZVecQuery
        {
            FieldName = WordsField,
            Fts = new ZVecFtsQuery { QueryString = query, DefaultOperator = ZVecFtsDefaultOperator.And },
        };
        var filterArgument = string.IsNullOrWhiteSpace(filter) ? null : filter;
        var docs = collection.Query(fts, topk: topK, filter: filterArgument, includeVector: false);
        var hits = new List<MediaSearchHit>(docs.Count);
        foreach (var doc in docs)
        {
            hits.Add(new MediaSearchHit(
                doc.Id,
                ReadField(doc, SourcePathField),
                ReadSeconds(doc, StartSecondsField),
                ReadSeconds(doc, EndSecondsField),
                doc.Score));
        }

        return hits;
    }

    public string StoredField(string collectionName, string contentId, string fieldName)
    {
        lock (sync)
        {
            return StoredFieldCore(collectionName, contentId, fieldName);
        }
    }

    private string StoredFieldCore(string collectionName, string contentId, string fieldName)
    {
        var collection = RequireCollection(collectionName);
        var doc = collection.Fetch(contentId, includeVector: false);
        return doc is null ? string.Empty : ReadField(doc, fieldName);
    }

    private IZvecCollection RequireCollection(string collectionName)
    {
        if (!MediaCollections.IsMedia(collectionName))
        {
            throw new ArgumentException("Only picture, video, and audio collections are media collections.", nameof(collectionName));
        }

        if (!collections.ContainsKey(collectionName))
        {
            CreateCore(collectionName, MediaCollections.WidthFor(collectionName));
        }

        return collections[collectionName];
    }

    private void OpenExistingCollections()
    {
        foreach (var collectionName in MediaCollections.WordCollections)
        {
            TryOpenCollection(collectionName, out _);
        }
    }

    private bool TryOpenCollection(string collectionName, out IZvecCollection? collection)
    {
        if (!MediaCollections.IsMedia(collectionName))
        {
            throw new ArgumentException("Only picture, video, and audio collections are media collections.", nameof(collectionName));
        }

        var rootPath = Path.Combine(databaseRoot, collectionName);
        if (!Directory.Exists(rootPath))
        {
            collection = null;
            return false;
        }

        if (!collections.ContainsKey(collectionName))
        {
            CreateCore(collectionName, MediaCollections.WidthFor(collectionName));
        }

        collection = collections[collectionName];
        return true;
    }

    private static string ReadField(ZVecDoc doc, string field) =>
        doc.Fields.TryGetValue(field, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

    private static double ReadSeconds(ZVecDoc doc, string field)
    {
        var text = ReadField(doc, field);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) ? seconds : 0d;
    }

    public void Dispose()
    {
        lock (sync)
        {
            DisposeCore();
        }
    }

    private void DisposeCore()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (var collection in collections.Values)
        {
            collection.Dispose();
        }

        collections.Clear();
        factory.Shutdown();
        factory.Dispose();
    }
}
