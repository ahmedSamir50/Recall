namespace Recall.Core.Ingest;

public sealed record GrantedTextDocument(string Path, string FileName, string Text, DateTimeOffset ModifiedAt);
