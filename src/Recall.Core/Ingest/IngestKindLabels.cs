using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public static class IngestKindLabels
{
    public static string Key(GrantFileKinds kind) =>
        kind switch
        {
            GrantFileKinds.Text => "Ingest_Text",
            GrantFileKinds.Code => "Ingest_Code",
            GrantFileKinds.Image => "Ingest_Image",
            GrantFileKinds.Video => "Ingest_Video",
            GrantFileKinds.Audio => "Ingest_Audio",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
}
