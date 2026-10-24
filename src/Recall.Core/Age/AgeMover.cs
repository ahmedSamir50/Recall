using Recall.Core.Checkpoints;
using Recall.Core.Faults;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Vectors;

namespace Recall.Core.Age;

public readonly record struct AgeStage(
    string LongId,
    string ShortId,
    string CollectionOld,
    float[] Vector768,
    string Words,
    string SourcePath,
    string FileName,
    string EncoderHash,
    bool PathLive,
    string GrantPath,
    string StartSeconds,
    string EndSeconds);

public static class AgeMover
{
    public static void Stage(IMediaVectorIndex index, IIngestCursorStore cursors, AgeStage stage)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(cursors);
        if (stage.Vector768.Length != MediaCollections.Width)
        {
            throw new RecallFaultException(RecallFault.EmbeddingWidthUnexpected);
        }

        var vector512 = MatryoshkaVectorMath.CutAndNormalize(stage.Vector768, MediaCollections.OldWidth);
        index.Create(stage.CollectionOld, MediaCollections.OldWidth);
        var insert = new MediaIndexInsert(
            stage.ShortId,
            stage.SourcePath,
            stage.FileName,
            stage.Words,
            vector512,
            stage.EncoderHash,
            stage.PathLive,
            stage.GrantPath,
            stage.StartSeconds,
            stage.EndSeconds);
        index.TryInsert(stage.CollectionOld, insert);
        cursors.CommitAgeCut(stage.LongId, stage.ShortId);
    }

    public static void Finish(IMediaVectorIndex index, IIngestCursorStore cursors, string longId)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(cursors);
        if (!index.TryDelete(longId))
        {
            return;
        }

        cursors.CompleteAgeCut(longId);
    }
}
