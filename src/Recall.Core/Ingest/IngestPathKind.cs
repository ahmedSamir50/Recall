using Recall.Core.Scope;

namespace Recall.Core.Ingest;

internal static class IngestPathKind
{
    public static bool TryGetKind(string path, out GrantFileKinds kind)
    {
        var fileName = Path.GetFileName(path);
        if (MediaFileKinds.IsImage(fileName))
        {
            kind = GrantFileKinds.Image;
            return true;
        }

        if (MediaFileKinds.IsVideo(fileName))
        {
            kind = GrantFileKinds.Video;
            return true;
        }

        if (MediaFileKinds.IsAudio(fileName))
        {
            kind = GrantFileKinds.Audio;
            return true;
        }

        if (FileAdmission.IsCodeFile(fileName))
        {
            kind = GrantFileKinds.Code;
            return true;
        }

        kind = GrantFileKinds.Text;
        return true;
    }
}
