namespace Recall.Core.Scope;

public enum ScopeChangeKind
{
    Created = 0,
    Modified = 1,
    Renamed = 2,
    Deleted = 3,
}

public sealed record ScopeChange(ScopeChangeKind Kind, string Path, string? PreviousPath);
