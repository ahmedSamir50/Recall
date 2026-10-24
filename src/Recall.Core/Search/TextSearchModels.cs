namespace Recall.Core.Search;

public sealed record TextIndexInsert(
    string ContentId,
    string SourcePath,
    string FileName,
    string RawNoteText,
    ReadOnlyMemory<float> Vector512,
    DateTimeOffset ModifiedAt,
    string EncoderHash,
    bool PathLive,
    string GrantPath,
    string FoldedKey,
    int Ordinal);

public sealed record TextSearchRequest(
    string Query,
    ReadOnlyMemory<float> QueryVector512,
    IReadOnlySet<string> GrantedPaths,
    string EncoderHash,
    string? Filter,
    bool UseInDatabaseRrf,
    int TopK = 64);

public sealed record TextSearchHit(
    string ContentId,
    string SourcePath,
    string FileName,
    DateTimeOffset ModifiedAt,
    TextMatchReason MatchReason,
    double Score,
    int Ordinal = 0,
    string RawNote = "",
    string GrantPath = "",
    bool SourceMissing = false,
    double StartSeconds = 0);

public sealed record VectorLiveState(string ContentId, bool PathLive, string EncoderHash, string GrantPath);
