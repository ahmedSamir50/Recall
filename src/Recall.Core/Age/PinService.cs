using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.Core.Vocabulary;

namespace Recall.Core.Age;

#pragma warning disable S3267
public sealed class PinService
{
    private readonly IIngestCursorStore cursors;
    private readonly IMediaVectorIndex index;
    private readonly IMediaFileFacts facts;
    private readonly IAgeCutDecision decision;
    private readonly IVisionFileIngest vision;
    private readonly IAudioFileIngest audio;
    private readonly LocalOpLogWriter opLog;
    private readonly AgeCutoffPreferenceService preferences;

    public PinService(
        IIngestCursorStore cursors,
        IMediaVectorIndex index,
        IMediaFileFacts facts,
        IAgeCutDecision decision,
        IVisionFileIngest vision,
        IAudioFileIngest audio,
        LocalOpLogWriter opLog,
        AgeCutoffPreferenceService preferences)
    {
        this.cursors = cursors;
        this.index = index;
        this.facts = facts;
        this.decision = decision;
        this.vision = vision;
        this.audio = audio;
        this.opLog = opLog;
        this.preferences = preferences;
    }

    public PinResult Pin(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        var modality = ResolveModality(path);
        if (modality is null)
        {
            return PinResult.NotMedia;
        }

        var mediaRow = cursors.FindMediaFile(path, modality.Value);
        if (mediaRow is null)
        {
            return PinResult.NotMedia;
        }

        if (!facts.Exists(path))
        {
            cursors.AddPin(path);
            return PinResult.MissingFileStays;
        }

        if (mediaRow.Width == MediaCollections.Width)
        {
            cursors.AddPin(path);
            var longId = FirstLongId(path, modality.Value);
            if (!string.IsNullOrEmpty(longId))
            {
                opLog.AppendPin(longId);
            }

            return PinResult.AlreadyLong;
        }

        cursors.AddPin(path);
        var grant = BuildGrant(path, modality.Value);
        return RegrowOffCaller(path, modality.Value, grant);
    }

    public PinResult Unpin(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        if (!cursors.IsPinned(path))
        {
            return PinResult.NotPinned;
        }

        var modality = ResolveModality(path);
        if (modality is null)
        {
            cursors.RemovePin(path);
            return PinResult.NotPinned;
        }

        var mediaRow = cursors.FindMediaFile(path, modality.Value);
        var longId = mediaRow is null ? string.Empty : FirstLongId(path, modality.Value);
        cursors.RemovePin(path);
        if (!string.IsNullOrEmpty(longId))
        {
            opLog.AppendUnpin(longId);
        }

        if (mediaRow is null)
        {
            return PinResult.UnpinStaysLong;
        }

        if (mediaRow.Width == MediaCollections.OldWidth)
        {
            return PinResult.UnpinReturnedShort;
        }

        var lastWrite = facts.LastWriteUtc(path);
        var years = preferences.Get();
        if (lastWrite is null || !FileAgePolicy.IsPastCutoff(lastWrite, years, DateTimeOffset.UtcNow))
        {
            return PinResult.UnpinStaysLong;
        }

        if (!decision.TryPass(modality.Value))
        {
            return PinResult.GateFailedStaysLong;
        }

        CutPathToShort(path, modality.Value);
        return PinResult.UnpinReturnedShort;
    }

    public void Resume()
    {
        foreach (var row in cursors.ListPinResumes())
        {
            if (!cursors.IsPinned(row.Path))
            {
                continue;
            }

            DeleteAllShortIds(row.Path, ResolveModality(row.Path));
            cursors.CompletePinResume(row.Path);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "Pin stays a synchronous port. Ingest runs on the thread pool so it does not capture the caller synchronization context.")]
    private PinResult RegrowOffCaller(string path, Modality modality, ScopeFolderGrant grant) =>
        Task.Run(() => RegrowAsync(path, modality, grant)).Result;

    private async Task<PinResult> RegrowAsync(string path, Modality modality, ScopeFolderGrant grant)
    {
        switch (modality)
        {
            case Modality.Image:
                await vision.IndexImageAsync(grant, path).ConfigureAwait(false);
                break;
            case Modality.Video:
                await vision.IndexVideoFramesAsync(grant, path).ConfigureAwait(false);
                break;
            case Modality.Audio:
                await audio.IndexAsync(grant, path).ConfigureAwait(false);
                break;
            default:
                cursors.RemovePin(path);
                return PinResult.NotMedia;
        }

        var contentHash = cursors.GetFile(path)?.ContentHash ?? string.Empty;
        var chunks = cursors.ListChunks(path).Where(c => !c.Retired && !c.Missing).OrderBy(c => c.Ordinal).ToList();
        if (chunks.Count == 0 || string.IsNullOrEmpty(contentHash))
        {
            cursors.RemovePin(path);
            return PinResult.NotMedia;
        }

        var firstLongId = string.Empty;
        var firstShortId = string.Empty;
        foreach (var chunk in chunks)
        {
            var longId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W768, chunk.Ordinal);
            if (!index.TryRead(longId, out _))
            {
                cursors.RemovePin(path);
                return PinResult.NotMedia;
            }

            if (string.IsNullOrEmpty(firstLongId))
            {
                firstLongId = longId;
                firstShortId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W512, chunk.Ordinal);
            }
        }

        opLog.AppendPin(firstLongId);
        cursors.StagePinResume(path, firstLongId, firstShortId);
        DeleteAllShortIds(path, modality);
        cursors.CompletePinResume(path);
        cursors.SetMediaWidth(path, modality, MediaCollections.Width);
        return PinResult.Regrown;
    }

    private void DeleteAllShortIds(string path, Modality? modality)
    {
        if (modality is null)
        {
            return;
        }

        var contentHash = cursors.GetFile(path)?.ContentHash ?? string.Empty;
        if (string.IsNullOrEmpty(contentHash))
        {
            return;
        }

        foreach (var chunk in cursors.ListChunks(path).Where(c => !c.Retired && !c.Missing))
        {
            var shortId = ContentIdBuilder.Build(modality.Value, contentHash, StoredWidth.W512, chunk.Ordinal);
            index.TryDelete(shortId);
        }
    }

    private void CutPathToShort(string path, Modality modality)
    {
        var contentHash = cursors.GetFile(path)?.ContentHash ?? string.Empty;
        var oldCollection = modality switch
        {
            Modality.Image => MediaCollections.ImageOld,
            Modality.Video => MediaCollections.VideoOld,
            Modality.Audio => MediaCollections.AudioOld,
            _ => throw new ArgumentOutOfRangeException(nameof(modality)),
        };

        foreach (var chunk in cursors.ListChunks(path).Where(c => !c.Retired && !c.Missing).OrderBy(c => c.Ordinal))
        {
            var longId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W768, chunk.Ordinal);
            if (!index.TryRead(longId, out var stored))
            {
                continue;
            }

            var shortId = ContentIdBuilder.Build(modality, contentHash, StoredWidth.W512, chunk.Ordinal);
            var stage = new AgeStage(
                longId,
                shortId,
                oldCollection,
                stored.Vector.ToArray(),
                stored.Words,
                stored.SourcePath,
                Path.GetFileName(stored.SourcePath),
                stored.EncoderHash,
                stored.PathLive,
                stored.GrantPath,
                stored.StartSeconds,
                stored.EndSeconds);
            AgeMover.Stage(index, cursors, stage);
            AgeMover.Finish(index, cursors, longId);
        }

        cursors.SetMediaWidth(path, modality, MediaCollections.OldWidth);
    }

    private static Modality? ResolveModality(string path)
    {
        if (MediaFileKinds.IsImage(path))
        {
            return Modality.Image;
        }

        if (MediaFileKinds.IsVideo(path))
        {
            return Modality.Video;
        }

        if (MediaFileKinds.IsAudio(path))
        {
            return Modality.Audio;
        }

        return null;
    }

    private string FirstLongId(string path, Modality modality)
    {
        var chunk = cursors.ListChunks(path).FirstOrDefault(c => !c.Retired && !c.Missing);
        if (chunk is null)
        {
            return string.Empty;
        }

        var contentHash = cursors.GetFile(path)?.ContentHash ?? string.Empty;
        return string.IsNullOrEmpty(contentHash)
            ? string.Empty
            : ContentIdBuilder.Build(modality, contentHash, StoredWidth.W768, chunk.Ordinal);
    }

    private ScopeFolderGrant BuildGrant(string path, Modality modality)
    {
        var kinds = modality switch
        {
            Modality.Image => GrantFileKinds.Image,
            Modality.Video => GrantFileKinds.Video,
            Modality.Audio => GrantFileKinds.Audio,
            _ => GrantFileKinds.None,
        };
        var grantPath = cursors.GetFile(path)?.GrantPath ?? path;
        return new ScopeFolderGrant(grantPath, Path.GetFileName(grantPath), kinds);
    }
}
#pragma warning restore S3267
