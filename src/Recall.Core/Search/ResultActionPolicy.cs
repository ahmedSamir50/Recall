using Recall.Core.Scope;

namespace Recall.Core.Search;

public enum ResultActionKind
{
    OpenOnly = 0,
    View = 1,
    Play = 2,
}

public static class ResultActionPolicy
{
    public static ResultActionKind Decide(string sourcePath)
    {
        if (MediaFileKinds.IsImage(sourcePath))
        {
            return ResultActionKind.View;
        }

        if (MediaFileKinds.IsVideo(sourcePath) || MediaFileKinds.IsAudio(sourcePath))
        {
            return ResultActionKind.Play;
        }

        return ResultActionKind.OpenOnly;
    }
}