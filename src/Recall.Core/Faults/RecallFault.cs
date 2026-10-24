namespace Recall.Core.Faults;

public enum RecallFault
{
    EmbeddingWidthUnexpected = 1,
    TextModelNotReady = 2,
    SearchPrefixAppliedTwice = 3,
    DocumentPrefixAppliedTwice = 4,
    StoredWidthBlocked = 5,
    TextWidthNot512 = 6,
    AgeCutoffNotAllowed = 7,
    GrantFileKindsEmpty = 8,
    DeviceIdNotReady = 9,
    DownloadHashMismatch = 10,
    DownloadIncomplete = 11,
    DownloadTransport = 12,
    TokenizerNativeUnsupported = 13,
    RrfRankConstant = 14,
    VectorSchemaStale = 20,
    AgeCutTextForbidden = 21,
    EgressProxyMissing = 22,
    EgressWatchIncomplete = 23,
}
