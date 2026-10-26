using Recall.Core.Checkpoints;
using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public sealed class IngestStatusService
{
    private static readonly GrantFileKinds[] KindOrder =
    [
        GrantFileKinds.Text,
        GrantFileKinds.Code,
        GrantFileKinds.Image,
        GrantFileKinds.Video,
        GrantFileKinds.Audio,
    ];

    private readonly IIngestCursorStore store;

    public IngestStatusService(IIngestCursorStore store)
    {
        this.store = store;
    }

    public IReadOnlyList<IngestKindCount> List(GrantFileKinds enabled)
    {
        if (enabled == GrantFileKinds.None)
        {
            return [];
        }

        var list = new List<IngestKindCount>();
        foreach (var kind in KindOrder)
        {
            if (!enabled.HasFlag(kind))
            {
                continue;
            }

            list.Add(store.Count(kind));
        }

        return list;
    }
}
