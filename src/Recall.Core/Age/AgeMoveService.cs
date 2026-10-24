using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Text;
using Recall.Core.Vocabulary;

namespace Recall.Core.Age;

#pragma warning disable S3267
public sealed class AgeMoveService
{
    private readonly IMediaVectorIndex index;
    private readonly IIngestCursorStore cursors;
    private readonly IEncoderFileHash hashes;
    private readonly IAgeCutDecision decision;
    private readonly IMediaFileFacts facts;
    private readonly AgeCutoffPreferenceService preferences;

    public AgeMoveService(
        IMediaVectorIndex index,
        IIngestCursorStore cursors,
        IEncoderFileHash hashes,
        IAgeCutDecision decision,
        IMediaFileFacts facts,
        AgeCutoffPreferenceService preferences)
    {
        this.index = index;
        this.cursors = cursors;
        this.hashes = hashes;
        this.decision = decision;
        this.facts = facts;
        this.preferences = preferences;
    }

    public bool TryMovePath(string path, Modality modality, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        Resume();
        if (modality == Modality.Text)
        {
            return false;
        }

        var encoderHash = EncoderHashFor(modality);
        if (string.IsNullOrEmpty(encoderHash))
        {
            return false;
        }

        var lastWrite = facts.LastWriteUtc(path);
        if (lastWrite is null)
        {
            return false;
        }

        var mediaRow = cursors.FindMediaFile(path, modality);
        if (mediaRow is null)
        {
            return false;
        }

        var years = preferences.Get();
        var chunks = cursors.ListChunks(path);
        if (chunks.Count == 0)
        {
            return false;
        }

        var contentHash = cursors.GetFile(path)?.ContentHash ?? string.Empty;
        if (string.IsNullOrEmpty(contentHash))
        {
            return false;
        }

        var oldCollection = OldCollection(modality);
        if (cursors.IsPinned(path))
        {
            foreach (var chunk in chunks.Where(c => !c.Retired && !c.Missing))
            {
                var longId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W768, chunk.Ordinal);
                var shortId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W512, chunk.Ordinal);
                if (index.TryRead(longId, out _) && index.TryRead(shortId, out _))
                {
                    index.TryDelete(shortId);
                }
            }

            return false;
        }

        var activeChunks = chunks.Where(c => !c.Retired && !c.Missing).OrderBy(c => c.Ordinal).ToList();
        var reconciledShortOnly = false;
        foreach (var chunk in activeChunks)
        {
            var longId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W768, chunk.Ordinal);
            var shortId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W512, chunk.Ordinal);
            var hasLong = index.TryRead(longId, out _);
            var hasShort = index.TryRead(shortId, out _);

            if (hasShort)
            {
                if (hasLong)
                {
                    index.TryDelete(longId);
                    reconciledShortOnly = true;
                }

                continue;
            }

            if (!FileAgePolicy.IsPastCutoff(lastWrite, years, utcNow))
            {
                return false;
            }

            if (!decision.TryPass(modality))
            {
                return false;
            }
        }

        var stages = new List<(AgeStage Stage, string LongId)>();
        foreach (var chunk in activeChunks)
        {
            var longId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W768, chunk.Ordinal);
            var shortId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W512, chunk.Ordinal);
            if (index.TryRead(shortId, out _))
            {
                continue;
            }

            if (!index.TryRead(longId, out var longVector))
            {
                return false;
            }

            stages.Add((
                new AgeStage(
                    longId,
                    shortId,
                    oldCollection,
                    longVector.Vector.ToArray(),
                    longVector.Words,
                    longVector.SourcePath,
                    Path.GetFileName(longVector.SourcePath),
                    longVector.EncoderHash,
                    longVector.PathLive,
                    longVector.GrantPath,
                    longVector.StartSeconds,
                    longVector.EndSeconds),
                longId));
        }

        foreach (var (stage, longId) in stages)
        {
            AgeMover.Stage(index, cursors, stage);
            AgeMover.Finish(index, cursors, longId);
        }

        if (stages.Count > 0)
        {
            cursors.SetMediaWidth(path, modality, MediaCollections.OldWidth);
            return true;
        }

        if (reconciledShortOnly)
        {
            cursors.SetMediaWidth(path, modality, MediaCollections.OldWidth);
        }

        return false;
    }

    public void Sweep(DateTimeOffset utcNow)
    {
        Resume();
        foreach (var row in cursors.ListMediaFiles())
        {
            TryMovePath(row.Path, row.Modality, utcNow);
        }
    }

    public void Resume()
    {
        foreach (var pending in cursors.ListAgeCuts())
        {
            AgeMover.Finish(index, cursors, pending.LongId);
        }
    }

    private string EncoderHashFor(Modality modality) =>
        modality switch
        {
            Modality.Image or Modality.Video => hashes.VisionHash ?? string.Empty,
            Modality.Audio => hashes.AudioHash ?? string.Empty,
            _ => string.Empty,
        };

    private static string OldCollection(Modality modality) =>
        modality switch
        {
            Modality.Image => MediaCollections.ImageOld,
            Modality.Video => MediaCollections.VideoOld,
            Modality.Audio => MediaCollections.AudioOld,
            _ => throw new ArgumentOutOfRangeException(nameof(modality)),
        };
}
#pragma warning restore S3267
