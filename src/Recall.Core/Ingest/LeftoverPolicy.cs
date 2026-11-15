namespace Recall.Core.Ingest;

public enum LeftoverAction
{
    Finish = 0,
    Missing = 1,
    Pause = 2,
}

public static class LeftoverPolicy
{
    public static LeftoverAction Decide(bool hasLiveChunks, bool fileExists)
    {
        if (hasLiveChunks)
        {
            return LeftoverAction.Finish;
        }

        if (!fileExists)
        {
            return LeftoverAction.Missing;
        }

        return LeftoverAction.Pause;
    }
}