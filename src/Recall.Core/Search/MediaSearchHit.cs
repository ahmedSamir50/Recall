namespace Recall.Core.Search;

public sealed record MediaSearchHit(
    string ContentId,
    string SourcePath,
    double StartSeconds,
    double EndSeconds,
    double Score);

public sealed record MediaSearchRequest(
    string CollectionName,
    ReadOnlyMemory<float> QueryVector768,
    string Filter,
    int TopK);
