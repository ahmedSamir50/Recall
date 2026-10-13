namespace Recall.Core.Scope;

public enum ScopeGrantAddStatus
{
    Added = 0,
    OverlapChildOfExistingParent = 1,
    Duplicate = 2,
}

public sealed record ScopeGrantAddResult(ScopeGrantAddStatus Status, string? Message = null);
