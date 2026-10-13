namespace Recall.Core.Search;

public sealed record TextIndexInsert(
    string ContentId,
    string SourcePath,
    string FileName,
    string RawNoteText,
    ReadOnlyMemory<float> Vector512,
    DateTimeOffset ModifiedAt);

public sealed record TextSearchRequest(
    string Query,
    ReadOnlyMemory<float> QueryVector512,
    IReadOnlySet<string> GrantedPaths,
    int TopK = 20);

public sealed record TextSearchHit(
    string ContentId,
    string SourcePath,
    string FileName,
    DateTimeOffset ModifiedAt,
    string MatchReason,
    double Score);
