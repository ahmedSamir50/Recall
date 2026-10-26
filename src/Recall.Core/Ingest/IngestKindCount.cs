using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public readonly record struct IngestKindCount(GrantFileKinds Kind, int Finished, int Seen)
{
    public int Percent => Seen == 0 ? 0 : Finished * 100 / Seen;
}
