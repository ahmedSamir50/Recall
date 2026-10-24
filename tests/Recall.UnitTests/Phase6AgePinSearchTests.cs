using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recall.Adapters.DependencyInjection;
using Recall.Adapters.Indexing;
using Recall.Core.DependencyInjection;
using Recall.Core.Age;
using Recall.Core.Checkpoints;
using Recall.Core.Faults;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Vectors;
using Recall.Core.Preferences;
using Recall.Core.Search;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.Core.Vocabulary;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class Phase6AgePinSearchTests
{
    private const string SampleHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void IsPastCutoff_Null_IsFalse()
    {
        var utcNow = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        FileAgePolicy.IsPastCutoff(null, AgeCutoffYears.Two, utcNow).Should().BeFalse();
    }

    [Fact]
    public void IsPastCutoff_ExactYears_IsFalse()
    {
        var utcNow = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var lastWrite = utcNow.AddYears(-2);
        FileAgePolicy.IsPastCutoff(lastWrite, AgeCutoffYears.Two, utcNow).Should().BeFalse();
    }

    [Fact]
    public void IsPastCutoff_OneTickOlder_IsTrue()
    {
        var utcNow = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var lastWrite = utcNow.AddYears(-2).AddTicks(-1);
        FileAgePolicy.IsPastCutoff(lastWrite, AgeCutoffYears.Two, utcNow).Should().BeTrue();
    }

    [Fact]
    public void IsPastCutoff_Younger_IsFalse()
    {
        var utcNow = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var lastWrite = utcNow.AddYears(-1);
        FileAgePolicy.IsPastCutoff(lastWrite, AgeCutoffYears.Two, utcNow).Should().BeFalse();
    }

    [Fact]
    public void IsPastCutoff_OtherYear_Throws()
    {
        var utcNow = DateTimeOffset.UtcNow;
        Action act = () => FileAgePolicy.IsPastCutoff(utcNow, (AgeCutoffYears)3, utcNow);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.AgeCutoffNotAllowed);
    }

    [Fact]
    public void Passes_Text_Throws()
    {
        Action act = () => AgeCutGate.Passes(Modality.Text, []);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.AgeCutTextForbidden);
    }

    [Fact]
    public void Passes_Null_Throws()
    {
        Action act = () => AgeCutGate.Passes(Modality.Image, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Passes_Empty_IsFalse()
    {
        AgeCutGate.Passes(Modality.Image, []).Should().BeFalse();
    }

    [Fact]
    public void Passes_BadLength_Throws()
    {
        var bad = new float[767];
        bad[0] = 1f;
        var items = new[] { new AgeCutItem(0, bad, bad) };
        Action act = () => AgeCutGate.Passes(Modality.Image, items);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.EmbeddingWidthUnexpected);
    }

    [Fact]
    public void AgeCut_RecallAt10_WithinFiveOfFullWidth()
    {
        var axis0 = UnitAxis(0);
        var axis1 = UnitAxis(1);
        var items = new[]
        {
            new AgeCutItem(0, axis0, axis0),
            new AgeCutItem(1, axis1, axis1),
        };
        AgeCutGate.Passes(Modality.Image, items).Should().BeTrue();
    }

    [Fact]
    public void AgeCutGate_FivePointDrop_Passes()
    {
        var items = new List<AgeCutItem>(20);
        for (var id = 0; id < 20; id++)
        {
            var stored = new float[768];
            var query = new float[768];
            if (id is >= 0 and <= 9 or 19)
            {
                stored[0] = 1f;
                query[0] = 1f;
                stored[512 + id] = 1f;
                query[512 + id] = 1f;
            }
            else
            {
                stored[id] = 1f;
                query[id] = 1f;
            }

            items.Add(new AgeCutItem(id, stored, query));
        }

        AgeCutGate.Passes(Modality.Image, items).Should().BeTrue();
    }

    [Fact]
    public void AgeMove_SyntheticNearDup_DoesNotPass()
    {
        var items = new List<AgeCutItem>(11);
        for (var id = 0; id <= 10; id++)
        {
            var v = new float[768];
            v[0] = 1f;
            v[512 + id] = 1f;
            items.Add(new AgeCutItem(id, v, v));
        }

        AgeCutGate.Passes(Modality.Image, items).Should().BeFalse();
    }

    [Fact]
    public void TryLoad_ImageVideoAudio_IsFalse()
    {
        AgeCutCorpus.TryLoad(Modality.Image, out var imageItems).Should().BeFalse();
        imageItems.Should().BeEmpty();
        AgeCutCorpus.TryLoad(Modality.Video, out var videoItems).Should().BeFalse();
        videoItems.Should().BeEmpty();
        AgeCutCorpus.TryLoad(Modality.Audio, out var audioItems).Should().BeFalse();
        audioItems.Should().BeEmpty();
    }

    [Fact]
    public void TryLoad_Text_Throws()
    {
        Action act = () => AgeCutCorpus.TryLoad(Modality.Text, out _);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.AgeCutTextForbidden);
    }

    [Fact]
    public void FailClosed_Image_IsFalse()
    {
        new FailClosedAgeCutDecision().TryPass(Modality.Image).Should().BeFalse();
    }

    [Fact]
    public void AgeMove_MissingCorpus_Stays768()
    {
        const string path = @"C:\G\old.jpg";
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var facts = new ConfigurableMediaFileFacts
        {
            ExistsValue = true,
            LastWriteUtcValue = utcNow.AddYears(-2).AddTicks(-1),
        };
        var (move, cursors, index, _, _) = CreateAgeMove(new FailClosedAgeCutDecision(), facts: facts);
        SeedLongImage(cursors, index, path, SampleHash);
        move.TryMovePath(path, Modality.Image, utcNow).Should().BeFalse();
        var longId = LongId(Modality.Image, SampleHash, 0);
        index.TryRead(longId, out _).Should().BeTrue();
        index.Inserts.Should().NotContain(i => i.ContentId == ShortId(Modality.Image, SampleHash, 0));
    }

    [Fact]
    public void AgeMove_EncoderMissing_Stays768()
    {
        const string path = @"C:\G\old.jpg";
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var facts = new ConfigurableMediaFileFacts { ExistsValue = true, LastWriteUtcValue = utcNow.AddYears(-2).AddTicks(-1) };
        var (move, cursors, index, _, _) = CreateAgeMove(new NeverCalledAgeCutDecision(), visionHash: null, facts: facts);
        SeedLongImage(cursors, index, path, SampleHash);
        move.TryMovePath(path, Modality.Image, utcNow).Should().BeFalse();
        index.TryRead(LongId(Modality.Image, SampleHash, 0), out _).Should().BeTrue();
    }

    [Fact]
    public void AgeMove_MissingTime_StaysLong()
    {
        const string path = @"C:\G\old.jpg";
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var facts = new ConfigurableMediaFileFacts { ExistsValue = true, LastWriteUtcValue = null };
        var (move, cursors, index, _, _) = CreateAgeMove(new PassThroughAgeCutDecision(), facts: facts);
        SeedLongImage(cursors, index, path, SampleHash);
        move.TryMovePath(path, Modality.Image, utcNow).Should().BeFalse();
        index.TryRead(LongId(Modality.Image, SampleHash, 0), out _).Should().BeTrue();
    }

    [Fact]
    public void AgeMove_YoungerTime_StaysShort()
    {
        const string path = @"C:\G\young.jpg";
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var facts = new ConfigurableMediaFileFacts { ExistsValue = true, LastWriteUtcValue = utcNow };
        var (move, cursors, index, _, _) = CreateAgeMove(new PassThroughAgeCutDecision(), facts: facts);
        SeedLongAndShortImage(cursors, index, path, SampleHash);
        move.TryMovePath(path, Modality.Image, utcNow).Should().BeFalse();
        index.TryRead(LongId(Modality.Image, SampleHash, 0), out _).Should().BeFalse();
        index.TryRead(ShortId(Modality.Image, SampleHash, 0), out _).Should().BeTrue();
    }

    [Fact]
    public void AgeMove_Text_ReturnsFalse()
    {
        var (move, _, index, _, _) = CreateAgeMove(new PassThroughAgeCutDecision());
        move.TryMovePath(@"C:\G\note.txt", Modality.Text, DateTimeOffset.UtcNow).Should().BeFalse();
        index.Inserts.Should().BeEmpty();
    }

    [Fact]
    public void AgeMove_CopiesAliasBeforeLongDelete()
    {
        const string path = @"C:\G\stage.jpg";
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var longId = LongId(Modality.Image, SampleHash, 0);
        var shortId = ShortId(Modality.Image, SampleHash, 0);
        SeedLongImage(cursors, index, path, SampleHash);
        cursors.AddAlias(longId, path);
        var vector = UnitAxis(0);
        var stage = new AgeStage(
            longId,
            shortId,
            MediaCollections.ImageOld,
            vector,
            "words",
            path,
            "stage.jpg",
            "vis",
            true,
            @"C:\G",
            string.Empty,
            string.Empty);
        AgeMover.Stage(index, cursors, stage);
        index.TryRead(longId, out _).Should().BeTrue();
        index.Inserts.Single(i => i.ContentId == shortId).Vector768.Length.Should().Be(MediaCollections.OldWidth);
        cursors.ListLiveAliases(shortId).Should().ContainSingle(a => a.Path == path);
        cursors.ListAgeCuts().Should().ContainSingle(p => p.LongId == longId && p.ShortId == shortId);
    }

    [Fact]
    public void AgeMove_DeletesLongRow_OneOldCollectionPerEncoder()
    {
        const string path = @"C:\G\finish.jpg";
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var longId = LongId(Modality.Image, SampleHash, 0);
        var shortId = ShortId(Modality.Image, SampleHash, 0);
        SeedLongImage(cursors, index, path, SampleHash);
        var stage = new AgeStage(
            longId,
            shortId,
            MediaCollections.ImageOld,
            UnitAxis(0),
            string.Empty,
            path,
            "finish.jpg",
            "vis",
            true,
            @"C:\G",
            string.Empty,
            string.Empty);
        AgeMover.Stage(index, cursors, stage);
        AgeMover.Finish(index, cursors, longId);
        index.TryRead(longId, out _).Should().BeFalse();
        index.TryRead(shortId, out _).Should().BeTrue();
        cursors.ListAgeCuts().Should().BeEmpty();
    }

    [Fact]
    public void AgeMove_DoesNotAppendInsertOrDelete()
    {
        const string path = @"C:\G\nolog.jpg";
        var opLog = new LocalOpLogWriter();
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var longId = LongId(Modality.Image, SampleHash, 0);
        var shortId = ShortId(Modality.Image, SampleHash, 0);
        SeedLongImage(cursors, index, path, SampleHash);
        var stage = new AgeStage(
            longId,
            shortId,
            MediaCollections.ImageOld,
            UnitAxis(0),
            string.Empty,
            path,
            "nolog.jpg",
            "vis",
            true,
            @"C:\G",
            string.Empty,
            string.Empty);
        AgeMover.Stage(index, cursors, stage);
        AgeMover.Finish(index, cursors, longId);
        opLog.Entries.Should().NotContain(e => e.Operation == OpLogOperation.Insert);
        opLog.Entries.Should().NotContain(e => e.Operation == OpLogOperation.Delete);
        _ = opLog;
    }

    [Fact]
    public void AgeMove_TwoFrames_BothShort()
    {
        const string path = @"C:\G\twoframe.mp4";
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var facts = new ConfigurableMediaFileFacts
        {
            ExistsValue = true,
            LastWriteUtcValue = utcNow.AddYears(-2).AddTicks(-1),
        };
        var (move, cursors, index, _, _) = CreateAgeMove(new PassThroughAgeCutDecision(), facts: facts);
        var long0 = LongId(Modality.Video, SampleHash, 0);
        var long1 = LongId(Modality.Video, SampleHash, 1);
        var short0 = ShortId(Modality.Video, SampleHash, 0);
        var short1 = ShortId(Modality.Video, SampleHash, 1);
        cursors.FinishPath(path, SampleHash, @"C:\G", "vis", [new ChunkWrite(long0, 0), new ChunkWrite(long1, 1)]);
        cursors.UpsertMediaFile(path, Modality.Video);
        InsertLong(index, MediaCollections.Video, long0, path, 0);
        InsertLong(index, MediaCollections.Video, long1, path, 1);
        move.TryMovePath(path, Modality.Video, utcNow).Should().BeTrue();
        index.TryRead(long0, out _).Should().BeFalse();
        index.TryRead(long1, out _).Should().BeFalse();
        index.TryRead(short0, out _).Should().BeTrue();
        index.TryRead(short1, out _).Should().BeTrue();
    }

    [Fact]
    public void MoveLiveAliases_DoesNotWriteAgeCut()
    {
        var cursors = new FakeIngestCursorStore();
        cursors.AddAlias("long-id", @"C:\G\a.jpg");
        cursors.MoveLiveAliases("long-id", "short-id");
        cursors.ListAgeCuts().Should().BeEmpty();
        cursors.ListLiveAliases("short-id").Should().ContainSingle();
    }

    [Fact]
    public void PinRegrow_MissingFile_DoesNotEmbed()
    {
        const string path = @"C:\G\missing.jpg";
        var facts = new ConfigurableMediaFileFacts { ExistsValue = false, LastWriteUtcValue = null };
        var vision = new RecordingVisionIngest();
        var (pin, cursors, _) = CreatePin(facts: facts, vision: vision);
        SeedShortImage(cursors, new FakeMediaVectorIndex(), path, SampleHash);
        pin.Pin(path).Should().Be(PinResult.MissingFileStays);
        vision.Calls.Should().Be(0);
    }

    [Fact]
    public void Pin_AlreadyLong_DoesNotEmbed()
    {
        const string path = @"C:\G\long.jpg";
        var vision = new RecordingVisionIngest();
        var (pin, cursors, index) = CreatePin(vision: vision);
        SeedLongImage(cursors, index, path, SampleHash);
        pin.Pin(path).Should().Be(PinResult.AlreadyLong);
        vision.Calls.Should().Be(0);
    }

    [Fact]
    public void PinRegrow_PresentFile_EmbedsOnce()
    {
        const string path = @"C:\G\short.jpg";
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var vision = new RegrowingVisionIngest(cursors, index);
        var (pin, _, _) = CreatePin(cursors: cursors, index: index, vision: vision);
        SeedShortImage(cursors, index, path, SampleHash);
        pin.Pin(path).Should().Be(PinResult.Regrown);
        vision.Calls.Should().Be(1);
        index.TryRead(ShortId(Modality.Image, SampleHash, 0), out _).Should().BeFalse();
    }

    [Fact]
    public void PinRegrow_PresentFile_WidthIs768()
    {
        const string path = @"C:\G\short-width.jpg";
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var vision = new RegrowingVisionIngest(cursors, index);
        var (pin, _, _) = CreatePin(cursors: cursors, index: index, vision: vision);
        SeedShortImage(cursors, index, path, SampleHash);
        pin.Pin(path).Should().Be(PinResult.Regrown);
        cursors.FindMediaFile(path, Modality.Image)!.Width.Should().Be(MediaCollections.Width);
    }

    [Fact]
    public async Task YoungerShort_Reingest_DoesNotWriteLong()
    {
        const string path = @"C:\G\young-short.jpg";
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var facts = new ConfigurableMediaFileFacts { ExistsValue = true, LastWriteUtcValue = utcNow };
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var hashes = new FakeEncoderFileHash { VisionHash = "vis", AudioHash = "aud", TextHash = "t" };
        var (move, _, _, _, _) = CreateAgeMove(new PassThroughAgeCutDecision(), facts: facts, cursors: cursors, index: index);
        SeedLongAndShortImage(cursors, index, path, SampleHash);
        move.TryMovePath(path, Modality.Image, utcNow).Should().BeFalse();
        index.TryRead(LongId(Modality.Image, SampleHash, 0), out _).Should().BeFalse();
        cursors.FindMediaFile(path, Modality.Image)!.Width.Should().Be(MediaCollections.OldWidth);
        var ingest = new VisionFileIngest(new FakeVisionEmbeddingEncoder(), index, hashes, cursors, ageMove: move);
        var grant = new ScopeFolderGrant(@"C:\G", "G", GrantFileKinds.Image);
        await ingest.IndexImageAsync(grant, path, TestContext.Current.CancellationToken);
        index.TryRead(LongId(Modality.Image, SampleHash, 0), out _).Should().BeFalse();
        index.TryRead(ShortId(Modality.Image, SampleHash, 0), out _).Should().BeTrue();
    }

    [Fact]
    public void PinRegrow_TwoFrames_DeletesBothShorts()
    {
        const string path = @"C:\G\twoframe-pin.mp4";
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var vision = new RegrowingVideoIngest(cursors, index);
        var (pin, _, _) = CreatePin(cursors: cursors, index: index, vision: vision);
        var long0 = LongId(Modality.Video, SampleHash, 0);
        var long1 = LongId(Modality.Video, SampleHash, 1);
        var short0 = ShortId(Modality.Video, SampleHash, 0);
        var short1 = ShortId(Modality.Video, SampleHash, 1);
        cursors.FinishPath(path, SampleHash, @"C:\G", "vis", [new ChunkWrite(long0, 0), new ChunkWrite(long1, 1)]);
        cursors.UpsertMediaFile(path, Modality.Video);
        cursors.SetMediaWidth(path, Modality.Video, MediaCollections.OldWidth);
        InsertShort(index, MediaCollections.VideoOld, short0, path);
        InsertShort(index, MediaCollections.VideoOld, short1, path);
        pin.Pin(path).Should().Be(PinResult.Regrown);
        index.TryRead(short0, out _).Should().BeFalse();
        index.TryRead(short1, out _).Should().BeFalse();
    }

    [Fact]
    public void Sweep_PinResume_DeletesShort()
    {
        const string path = @"C:\G\sweep-pin.jpg";
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var (move, _, _, _, _) = CreateAgeMove(new FailClosedAgeCutDecision(), cursors: cursors, index: index);
        var (pin, _, _) = CreatePin(cursors: cursors, index: index);
        var longId = LongId(Modality.Image, SampleHash, 0);
        var shortId = ShortId(Modality.Image, SampleHash, 0);
        SeedLongImage(cursors, index, path, SampleHash);
        InsertShort(index, MediaCollections.ImageOld, shortId, path);
        cursors.AddPin(path);
        cursors.StagePinResume(path, longId, shortId);
        pin.Resume();
        move.Sweep(DateTimeOffset.UtcNow);
        index.TryRead(shortId, out _).Should().BeFalse();
    }

    [Fact]
    public void AgeMove_SecondChunkMissingVector_StagesNothing()
    {
        const string path = @"C:\G\partial.mp4";
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var facts = new ConfigurableMediaFileFacts
        {
            ExistsValue = true,
            LastWriteUtcValue = utcNow.AddYears(-2).AddTicks(-1),
        };
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var (move, _, _, _, _) = CreateAgeMove(new PassThroughAgeCutDecision(), facts: facts, cursors: cursors, index: index);
        var long0 = LongId(Modality.Video, SampleHash, 0);
        var long1 = LongId(Modality.Video, SampleHash, 1);
        var short0 = ShortId(Modality.Video, SampleHash, 0);
        var short1 = ShortId(Modality.Video, SampleHash, 1);
        cursors.FinishPath(path, SampleHash, @"C:\G", "vis", [new ChunkWrite(long0, 0), new ChunkWrite(long1, 1)]);
        cursors.UpsertMediaFile(path, Modality.Video);
        InsertLong(index, MediaCollections.Video, long0, path, 0);
        move.TryMovePath(path, Modality.Video, utcNow).Should().BeFalse();
        index.TryRead(long0, out _).Should().BeTrue();
        index.TryRead(long1, out _).Should().BeFalse();
        index.TryRead(short0, out _).Should().BeFalse();
        index.TryRead(short1, out _).Should().BeFalse();
        cursors.ListAgeCuts().Should().BeEmpty();
    }

    [Fact]
    public void Resume_ClosedCollection_DeletesLongAndKeepsPendingWhenDeleteMisses()
    {
        const string path = @"C:\G\closed.jpg";
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_zvec_" + Guid.NewGuid().ToString("N"));
        var longId = LongId(Modality.Image, SampleHash, 0);
        var shortId = ShortId(Modality.Image, SampleHash, 0);
        var cursors = new FakeIngestCursorStore();
        var stage = new AgeStage(
            longId,
            shortId,
            MediaCollections.ImageOld,
            UnitAxis(0),
            string.Empty,
            path,
            "closed.jpg",
            "vis",
            true,
            @"C:\G",
            string.Empty,
            string.Empty);
        try
        {
            using (var index = new ZVecMediaVectorIndex(root))
            {
                InsertLongZvec(index, MediaCollections.Image, longId, path);
                AgeMover.Stage(index, cursors, stage);
                cursors.ListAgeCuts().Should().ContainSingle();
            }

            using (var reopened = new ZVecMediaVectorIndex(root))
            {
                var fileFacts = new ConfigurableMediaFileFacts();
                var preferences = new AgeCutoffPreferenceService(new FakeAgeCutoffPreferenceStore());
                var hashes = new FakeEncoderFileHash { VisionHash = "vis", AudioHash = "aud", TextHash = "t" };
                var move = new AgeMoveService(reopened, cursors, hashes, new FailClosedAgeCutDecision(), fileFacts, preferences);
                move.Resume();
                reopened.TryRead(longId, out _).Should().BeFalse();
                cursors.ListAgeCuts().Should().BeEmpty();
            }

            var missCursors = new FakeIngestCursorStore();
            using var missIndex = new ZVecMediaVectorIndex(Path.Combine(root, "miss"));
            AgeMover.Stage(missIndex, missCursors, stage);
            AgeMover.Finish(missIndex, missCursors, longId);
            missCursors.ListAgeCuts().Should().ContainSingle(p => p.LongId == longId);
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public async Task Reingest_ExistingLongId_WritesMediaFile()
    {
        var bytes = new byte[] { 9, 8, 7, 6, 5 };
        var path = Path.Combine(Path.GetTempPath(), "recall_existing_long_" + Guid.NewGuid().ToString("N") + ".jpg");
        await File.WriteAllBytesAsync(path, bytes, TestContext.Current.CancellationToken);
        try
        {
            var cursors = new FakeIngestCursorStore();
            var index = new FakeMediaVectorIndex();
            var hashes = new FakeEncoderFileHash { VisionHash = "vis", AudioHash = "aud", TextHash = "t" };
            var contentHash = ContentIdBuilder.HashBytes(bytes);
            var longId = LongId(Modality.Image, contentHash, 0);
            cursors.FinishPath(path, contentHash, Path.GetDirectoryName(path) ?? path, "vis", [new ChunkWrite(longId, 0)]);
            InsertLong(index, MediaCollections.Image, longId, path, 0);
            cursors.FindMediaFile(path, Modality.Image).Should().BeNull();
            var ingest = new VisionFileIngest(new FakeVisionEmbeddingEncoder(), index, hashes, cursors);
            var grant = new ScopeFolderGrant(Path.GetDirectoryName(path) ?? path, "G", GrantFileKinds.Image);
            await ingest.IndexImageAsync(grant, path, TestContext.Current.CancellationToken);
            cursors.FindMediaFile(path, Modality.Image).Should().NotBeNull();
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void Pin_NotMedia_ReturnsNotMedia()
    {
        var (pin, _, _) = CreatePin();
        pin.Pin(@"C:\G\note.txt").Should().Be(PinResult.NotMedia);
    }

    [Fact]
    public void Pin_Blank_Throws()
    {
        var (pin, _, _) = CreatePin();
        Action act = () => pin.Pin("  ");
        act.Should().Throw<ArgumentException>().WithMessage("Path must not be blank.*");
    }

    [Fact]
    public void Pin_AudioOff_RemovesPin()
    {
        const string path = @"C:\G\track.wav";
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var audio = new FakeAudioFileIngest();
        var (pin, _, _) = CreatePin(cursors: cursors, index: index, audio: audio);
        SeedShortImage(cursors, index, path, SampleHash, Modality.Audio);
        pin.Pin(path).Should().Be(PinResult.NotMedia);
        cursors.IsPinned(path).Should().BeFalse();
    }

    [Fact]
    public void PinResume_PinStillOn_DeletesShort()
    {
        const string path = @"C:\G\resume.jpg";
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var (pin, _, _) = CreatePin(cursors: cursors, index: index);
        var longId = LongId(Modality.Image, SampleHash, 0);
        var shortId = ShortId(Modality.Image, SampleHash, 0);
        SeedLongImage(cursors, index, path, SampleHash);
        InsertShort(index, MediaCollections.ImageOld, shortId, path);
        cursors.AddPin(path);
        cursors.StagePinResume(path, longId, shortId);
        pin.Resume();
        index.TryRead(shortId, out _).Should().BeFalse();
    }

    [Fact]
    public void PinResume_PinOff_KeepsShort()
    {
        const string path = @"C:\G\resume-off.jpg";
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var (pin, _, _) = CreatePin(cursors: cursors, index: index);
        var longId = LongId(Modality.Image, SampleHash, 0);
        var shortId = ShortId(Modality.Image, SampleHash, 0);
        InsertShort(index, MediaCollections.ImageOld, shortId, path);
        cursors.StagePinResume(path, longId, shortId);
        pin.Resume();
        index.TryRead(shortId, out _).Should().BeTrue();
    }

    [Fact]
    public void Unpin_OldFile_ReturnsShort()
    {
        const string path = @"C:\G\unpin-old.jpg";
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var facts = new ConfigurableMediaFileFacts
        {
            ExistsValue = true,
            LastWriteUtcValue = utcNow.AddYears(-2).AddTicks(-1),
        };
        var (pin, cursors, index) = CreatePin(decision: new PassThroughAgeCutDecision(), facts: facts);
        SeedLongImage(cursors, index, path, SampleHash);
        cursors.AddPin(path);
        pin.Unpin(path).Should().Be(PinResult.UnpinReturnedShort);
        index.TryRead(LongId(Modality.Image, SampleHash, 0), out _).Should().BeFalse();
        index.TryRead(ShortId(Modality.Image, SampleHash, 0), out _).Should().BeTrue();
    }

    [Fact]
    public void Unpin_GateFails_StaysLong()
    {
        const string path = @"C:\G\gate.jpg";
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var facts = new ConfigurableMediaFileFacts
        {
            ExistsValue = true,
            LastWriteUtcValue = utcNow.AddYears(-2).AddTicks(-1),
        };
        var (pin, cursors, index) = CreatePin(decision: new FailClosedAgeCutDecision(), facts: facts);
        SeedLongImage(cursors, index, path, SampleHash);
        cursors.AddPin(path);
        pin.Unpin(path).Should().Be(PinResult.GateFailedStaysLong);
        index.TryRead(LongId(Modality.Image, SampleHash, 0), out _).Should().BeTrue();
    }

    [Fact]
    public void Unpin_YoungFile_StaysLong()
    {
        const string path = @"C:\G\young-pin.jpg";
        var facts = new ConfigurableMediaFileFacts { ExistsValue = true, LastWriteUtcValue = DateTimeOffset.UtcNow };
        var (pin, cursors, index) = CreatePin(decision: new PassThroughAgeCutDecision(), facts: facts);
        SeedLongImage(cursors, index, path, SampleHash);
        cursors.AddPin(path);
        pin.Unpin(path).Should().Be(PinResult.UnpinStaysLong);
        index.TryRead(LongId(Modality.Image, SampleHash, 0), out _).Should().BeTrue();
    }

    [Fact]
    public void Unpin_NotPinned_ReturnsNotPinned()
    {
        var (pin, _, _) = CreatePin();
        pin.Unpin(@"C:\G\free.jpg").Should().Be(PinResult.NotPinned);
    }

    [Fact]
    public void AppendPin_WritesPinLine()
    {
        const string path = @"C:\G\oplog-pin.jpg";
        var dir = Path.Combine(Path.GetTempPath(), "recall_phase6_oplog_" + Guid.NewGuid().ToString("N"));
        var opLog = new LocalOpLogWriter(dir);
        var cursors = new FakeIngestCursorStore();
        var index = new FakeMediaVectorIndex();
        var (pin, _, _) = CreatePin(cursors: cursors, index: index, opLog: opLog);
        SeedLongImage(cursors, index, path, SampleHash);
        pin.Pin(path);
        var logFile = Directory.GetFiles(dir, "*.log").Single();
        File.ReadAllText(logFile).Should().Contain("\tPin\t");
    }

    [Fact]
    public void AgeCutoffActions_Set_Sweeps()
    {
        var store = new FakeAgeCutoffPreferenceStore { Value = AgeCutoffYears.Two };
        var preferences = new AgeCutoffPreferenceService(store);
        var (move, _, _, _, _) = CreateAgeMove(new FailClosedAgeCutDecision(), preferenceStore: store);
        Action act = () => AgeCutoffActions.Set(preferences, move, AgeCutoffYears.Five, DateTimeOffset.UtcNow);
        act.Should().NotThrow();
        store.Value.Should().Be(AgeCutoffYears.Five);
    }

    [Fact]
    public void AgeCutoffActions_BadYear_ThrowsAndKeepsStore()
    {
        var store = new FakeAgeCutoffPreferenceStore { Value = AgeCutoffYears.Two };
        var preferences = new AgeCutoffPreferenceService(store);
        var (move, _, _, _, _) = CreateAgeMove(new FailClosedAgeCutDecision(), preferenceStore: store);
        Action act = () => AgeCutoffActions.Set(preferences, move, (AgeCutoffYears)3, DateTimeOffset.UtcNow);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.AgeCutoffNotAllowed);
        store.Value.Should().Be(AgeCutoffYears.Two);
    }

    [Fact]
    public void Create_OldImage_Width512_Opens()
    {
        ZVecMediaVectorIndex.BuildSchema(MediaCollections.ImageOld, MediaCollections.OldWidth)
            .Vectors.Single(v => v.Name == ZVecMediaVectorIndex.EmbeddingField)
            .Dimension.Should().Be(512);
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_zvec_" + Guid.NewGuid().ToString("N"));
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            var act = () => index.Create(MediaCollections.ImageOld, MediaCollections.OldWidth);
            act.Should().NotThrow();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Create_OldImage_Width768_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_zvec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            var act = () => index.Create(MediaCollections.ImageOld, MediaCollections.Width);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Create_LongImage_Width512_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_zvec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            var act = () => index.Create(MediaCollections.Image, MediaCollections.OldWidth);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryInsert_WidthMismatch_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_zvec_" + Guid.NewGuid().ToString("N"));
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            index.Create(MediaCollections.ImageOld, MediaCollections.OldWidth);
            var vector = new float[MediaCollections.Width];
            vector[0] = 1f;
            var insert = new MediaIndexInsert(
                ShortId(Modality.Image, SampleHash, 0),
                @"C:\G\a.jpg",
                "a.jpg",
                string.Empty,
                vector,
                "vis",
                true,
                @"C:\G",
                string.Empty,
                string.Empty);
            var act = () => index.TryInsert(MediaCollections.ImageOld, insert);
            act.Should().Throw<ArgumentException>().WithMessage("Vector length must match the collection width.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Search_MissingOldCollection_IsEmpty()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_zvec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            index.Create(MediaCollections.Image, MediaCollections.Width);
            var query = new float[MediaCollections.Width];
            var request = new MediaSearchRequest(MediaCollections.ImageOld, query, string.Empty, 10);
            index.Search(request, TestContext.Current.CancellationToken).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Search_UnknownName_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_zvec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            var query = new float[MediaCollections.Width];
            var request = new MediaSearchRequest("not_media", query, string.Empty, 10);
            var act = () => index.Search(request);
            act.Should().Throw<ArgumentException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryRead_Missing_IsFalse()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_zvec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            index.TryRead("missing-id", out _).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryRead_Inserted_Returns768()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_zvec_" + Guid.NewGuid().ToString("N"));
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            index.Create(MediaCollections.Image, MediaCollections.Width);
            var longId = LongId(Modality.Image, SampleHash, 0);
            var vector = UnitAxis(0);
            var insert = new MediaIndexInsert(
                longId,
                @"C:\G\read.jpg",
                "read.jpg",
                string.Empty,
                vector,
                "vis",
                true,
                @"C:\G",
                string.Empty,
                string.Empty);
            index.TryInsert(MediaCollections.Image, insert).Should().BeTrue();
            index.TryRead(longId, out var stored).Should().BeTrue();
            stored.Vector.Length.Should().Be(768);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Fuse_EqualWeight_TieKeepsEarlierStart()
    {
        var longList = new[]
        {
            new MediaSearchHit("b-long", "b", 1, 2, 0.5),
            new MediaSearchHit("a-long", "a", 4, 5, 0.4),
        };
        var oldList = new[]
        {
            new MediaSearchHit("a-old", "a", 4, 5, 0.5),
            new MediaSearchHit("b-old", "b", 1, 2, 0.4),
        };
        var fused = AgeRrf.Fuse([longList, oldList]);
        fused.Select(h => h.SourcePath).Should().Equal("b", "a");
        var expected = 1d / 61d + 1d / 62d;
        fused.Should().OnlyContain(h => Math.Abs(h.Score - expected) < 1e-12);
    }

    [Fact]
    public void Fuse_Null_Throws()
    {
        Action act = () => AgeRrf.Fuse(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Fuse_Empty_IsEmpty()
    {
        AgeRrf.Fuse([]).Should().BeEmpty();
        AgeRrf.Fuse([[], []]).Should().BeEmpty();
    }

    [Fact]
    public void Fuse_DuplicatePath_UsesFirstRank()
    {
        var list = new[]
        {
            new MediaSearchHit("first", "dup", 0, 1, 0.9),
            new MediaSearchHit("second", "dup", 2, 3, 0.1),
        };
        AgeRrf.Fuse([list]).Should().ContainSingle().Which.ContentId.Should().Be("first");
    }

    [Fact]
    public void Join_Order_IsOcrTranscriptKeywords()
    {
        MediaWords.Join("ا", "talk", "lens").Should().Be("ا\ntalk\nlens");
    }

    [Fact]
    public void Join_SkipsBlank()
    {
        MediaWords.Join(null, "talk", "  ").Should().Be("talk");
    }

    [Fact]
    public void Join_AllBlank_IsEmpty()
    {
        MediaWords.Join(null, " ", null).Should().Be(string.Empty);
    }

    [Fact]
    public async Task PictureQuery_EmbedsOnce_SearchesLongAndOld()
    {
        var encoder = new FakeVisionEmbeddingEncoder();
        var index = new FakeMediaVectorIndex
        {
            OnSearch = request =>
            {
                if (MediaCollections.WidthFor(request.CollectionName) == MediaCollections.OldWidth)
                {
                    request.QueryVector768.Length.Should().Be(MediaCollections.OldWidth);
                }

                return [];
            },
        };
        var service = new PictureSearchService(encoder, index, new FakeEncoderFileHash { VisionHash = "vis" });
        var grants = new[] { new ScopeFolderGrant(@"C:\G", "G", GrantFileKinds.Image) };
        await service.FindAsync(new byte[] { 1, 2, 3 }, grants, TestContext.Current.CancellationToken);
        encoder.Calls.Should().Be(1);
        index.QueriedCollections.Should().Equal(MediaCollections.PictureCollections);
    }

    [Fact]
    public async Task AudioQuery_EmbedsOnce_SearchesLongAndOld()
    {
        var encoder = new FakeAudioEmbeddingEncoder();
        var index = new FakeMediaVectorIndex();
        var service = new AudioSearchService(encoder, index, new FakeEncoderFileHash { AudioHash = "aud" });
        var grants = new[] { new ScopeFolderGrant(@"C:\G", "G", GrantFileKinds.Audio) };
        await service.FindAsync(IngestTestKit.SixteenKFloats, grants, TestContext.Current.CancellationToken);
        encoder.EmbedCalls.Should().Be(1);
        index.QueriedCollections.Should().Equal(MediaCollections.AudioCollections);
    }

    [Fact]
    public async Task TextQuery_WordlessPicture_IsAbsent()
    {
        const string picturePath = @"D:\pics\empty.jpg";
        var media = new FakeMediaVectorIndex
        {
            OnSearchWords = (_, _, _, _) => [],
        };
        var service = IngestTestKit.CreateSearchService(
            new FakeTextEmbeddingEncoder(),
            new FakeTextVectorIndex(),
            mediaIndex: media,
            encoderFileHash: new FakeEncoderFileHash { TextHash = "t", VisionHash = "vis", AudioHash = "aud" });
        var grants = new[] { new ScopeFolderGrant(@"D:\pics", "pics", GrantFileKinds.Image | GrantFileKinds.Text) };
        var hits = await service.FindAsync("lens", grants, TestContext.Current.CancellationToken);
        hits.Should().NotContain(h => h.SourcePath == picturePath);
        media.WordsRequests.Should().Contain(w => w.Collection == MediaCollections.Image);
    }

    [Fact]
    public async Task TextQuery_OcrWords_ReturnsMediaPath()
    {
        const string scanned = @"D:\scan\page.jpg";
        var media = new FakeMediaVectorIndex
        {
            OnSearchWords = (collection, _, _, _) =>
                collection == MediaCollections.Image
                    ? [new MediaSearchHit("id", scanned, 0, 0, 0.8)]
                    : [],
        };
        var service = IngestTestKit.CreateSearchService(
            new FakeTextEmbeddingEncoder(),
            new FakeTextVectorIndex(),
            mediaIndex: media,
            encoderFileHash: new FakeEncoderFileHash { TextHash = "t", VisionHash = "vis", AudioHash = "aud" });
        var grants = new[] { new ScopeFolderGrant(@"D:\scan", "scan", GrantFileKinds.Image | GrantFileKinds.Text) };
        var hits = await service.FindAsync("invoice", grants, TestContext.Current.CancellationToken);
        hits.Should().Contain(h => h.SourcePath == scanned);
    }

    [Fact]
    public async Task TextQuery_PictureWords_UseVisionHash()
    {
        var media = new FakeMediaVectorIndex();
        var service = IngestTestKit.CreateSearchService(
            new FakeTextEmbeddingEncoder(),
            new FakeTextVectorIndex(),
            mediaIndex: media,
            encoderFileHash: new FakeEncoderFileHash { TextHash = "t", VisionHash = "vis", AudioHash = "aud" });
        var grants = new[] { new ScopeFolderGrant(@"D:\G", "G", GrantFileKinds.Image | GrantFileKinds.Text) };
        await service.FindAsync("word", grants, TestContext.Current.CancellationToken);
        media.WordsRequests.Should().Contain(w => w.Collection == MediaCollections.Image && w.Filter.Contains("vis"));
        media.WordsRequests.Should().Contain(w => w.Collection == MediaCollections.Audio && w.Filter.Contains("aud"));
    }

    [Fact]
    public async Task TextQuery_DoesNotSearchMediaDense()
    {
        var media = new FakeMediaVectorIndex();
        var service = IngestTestKit.CreateSearchService(
            new FakeTextEmbeddingEncoder(),
            new FakeTextVectorIndex(),
            mediaIndex: media,
            encoderFileHash: new FakeEncoderFileHash { TextHash = "t", VisionHash = "vis", AudioHash = "aud" });
        var grants = new[] { new ScopeFolderGrant(@"D:\G", "G", GrantFileKinds.Image | GrantFileKinds.Text) };
        await service.FindAsync("word", grants, TestContext.Current.CancellationToken);
        media.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task TextQuery_ImageGrantOnly_SearchesPictureWords()
    {
        var media = new FakeMediaVectorIndex
        {
            OnSearchWords = (collection, _, _, _) =>
                collection == MediaCollections.Image
                    ? [new MediaSearchHit("id", @"D:\G\pic.jpg", 0, 0, 0.5)]
                    : [],
        };
        var service = IngestTestKit.CreateSearchService(
            new FakeTextEmbeddingEncoder(),
            new FakeTextVectorIndex(),
            mediaIndex: media,
            encoderFileHash: new FakeEncoderFileHash { VisionHash = "vis" });
        var grants = new[] { new ScopeFolderGrant(@"D:\G", "G", GrantFileKinds.Image) };
        var hits = await service.FindAsync("pic", grants, TestContext.Current.CancellationToken);
        hits.Should().ContainSingle();
    }

    [Fact]
    public async Task TextQuery_TwentyTextHits_SkipsMediaWords()
    {
        var textIndex = new TwentyHitTextIndex();
        var media = new FakeMediaVectorIndex();
        var service = IngestTestKit.CreateSearchService(
            new FakeTextEmbeddingEncoder(),
            textIndex,
            mediaIndex: media,
            encoderFileHash: new FakeEncoderFileHash { TextHash = "t", VisionHash = "vis", AudioHash = "aud" });
        var grants = new[] { new ScopeFolderGrant(@"D:\G", "G", GrantFileKinds.Image | GrantFileKinds.Text) };
        var hits = await service.FindAsync("word", grants, TestContext.Current.CancellationToken);
        hits.Should().HaveCount(20);
        media.WordsRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task TextQuery_SamePath_DoesNotDuplicate()
    {
        const string shared = @"D:\G\both.txt";
        var textIndex = new SingleHitTextIndex(shared);
        var media = new FakeMediaVectorIndex
        {
            OnSearchWords = (_, _, _, _) => [new MediaSearchHit("mid", shared, 0, 0, 0.4)],
        };
        var service = IngestTestKit.CreateSearchService(
            new FakeTextEmbeddingEncoder(),
            textIndex,
            mediaIndex: media,
            encoderFileHash: new FakeEncoderFileHash { TextHash = "t", VisionHash = "vis", AudioHash = "aud" });
        var grants = new[] { new ScopeFolderGrant(@"D:\G", "G", GrantFileKinds.Image | GrantFileKinds.Text) };
        var hits = await service.FindAsync("word", grants, TestContext.Current.CancellationToken);
        hits.Should().HaveCount(1);
    }

    [Fact]
    public async Task PictureQuery_Cancelled_Throws()
    {
        var encoder = new FakeVisionEmbeddingEncoder();
        var index = new FakeMediaVectorIndex();
        var service = new PictureSearchService(encoder, index, new FakeEncoderFileHash { VisionHash = "vis" });
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var act = async () => await service.FindAsync(
            new byte[] { 1 },
            [new ScopeFolderGrant(@"C:\G", "G", GrantFileKinds.Image)],
            cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        encoder.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Scheduler_MediaPass_SweepsWhenQueueEmpty()
    {
        var scheduler = new IngestScheduler();
        var runner = new IngestSchedulerRunner(scheduler);
        scheduler.Enqueue(new IngestJob(IngestModel.Media, "pic.jpg"));
        var sweepCount = 0;
        var hooks = new IngestPassHooks
        {
            SweepAgedMedia = _ => sweepCount++,
        };
        await runner.RunReadyPassesAsync(DateTimeOffset.UtcNow, hooks, TestContext.Current.CancellationToken);
        sweepCount.Should().Be(0);
        await runner.RunReadyPassesAsync(
            DateTimeOffset.UtcNow + IngestScheduler.OpenIdleDelay,
            hooks,
            TestContext.Current.CancellationToken);
        sweepCount.Should().Be(1);
        scheduler.QueueCount(IngestModel.Media).Should().Be(0);
    }

    [Fact]
    public void Host_ResolvesAgeMoveAndPin_DoesNotCycle()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase6_host_" + Guid.NewGuid().ToString("N"));
        var modelDir = Path.Combine(root, "models");
        var vectorRoot = Path.Combine(root, "vectors");
        Directory.CreateDirectory(modelDir);
        Directory.CreateDirectory(vectorRoot);
        try
        {
            var services = new ServiceCollection();
            services.AddRecallCore();
            services.AddSingleton<IAgeCutoffPreferenceStore, FakeAgeCutoffPreferenceStore>();
            services.AddSingleton<IIngestCursorStore>(_ => new SqliteIngestCursorStore(Path.Combine(vectorRoot, CheckpointSql.FileName)));
            services.AddRecallAdapters(modelDir, vectorRoot);
            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<AgeMoveService>().Should().NotBeNull();
            provider.GetRequiredService<PinService>().Should().NotBeNull();
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void CommitAgeCut_BlankId_DoesNotOpenARow()
    {
        var db = Path.Combine(Path.GetTempPath(), "recall_phase6_sql_" + Guid.NewGuid().ToString("N"), CheckpointSql.FileName);
        try
        {
            var store = new SqliteIngestCursorStore(db);
            Action act = () => store.CommitAgeCut(string.Empty, "short-id");
            act.Should().Throw<ArgumentException>();
            store.ListAgeCuts().Should().BeEmpty();
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    private static (AgeMoveService Move, FakeIngestCursorStore Cursors, FakeMediaVectorIndex Index, ConfigurableMediaFileFacts Facts, FakeAgeCutoffPreferenceStore Store) CreateAgeMove(
        IAgeCutDecision decision,
        ConfigurableMediaFileFacts? facts = null,
        FakeAgeCutoffPreferenceStore? preferenceStore = null,
        string? visionHash = "vis",
        string? audioHash = "aud",
        FakeIngestCursorStore? cursors = null,
        FakeMediaVectorIndex? index = null)
    {
        cursors ??= new FakeIngestCursorStore();
        index ??= new FakeMediaVectorIndex();
        var fileFacts = facts ?? new ConfigurableMediaFileFacts { ExistsValue = true, LastWriteUtcValue = DateTimeOffset.UtcNow.AddYears(-3) };
        var store = preferenceStore ?? new FakeAgeCutoffPreferenceStore();
        var preferences = new AgeCutoffPreferenceService(store);
        var hashes = new FakeEncoderFileHash { VisionHash = visionHash, AudioHash = audioHash, TextHash = "t" };
        var move = new AgeMoveService(index, cursors, hashes, decision, fileFacts, preferences);
        return (move, cursors, index, fileFacts, store);
    }

    private static (PinService Pin, FakeIngestCursorStore Cursors, FakeMediaVectorIndex Index) CreatePin(
        FakeIngestCursorStore? cursors = null,
        FakeMediaVectorIndex? index = null,
        ConfigurableMediaFileFacts? facts = null,
        IAgeCutDecision? decision = null,
        IVisionFileIngest? vision = null,
        IAudioFileIngest? audio = null,
        LocalOpLogWriter? opLog = null)
    {
        cursors ??= new FakeIngestCursorStore();
        index ??= new FakeMediaVectorIndex();
        var fileFacts = facts ?? new ConfigurableMediaFileFacts { ExistsValue = true, LastWriteUtcValue = DateTimeOffset.UtcNow };
        var preferences = new AgeCutoffPreferenceService(new FakeAgeCutoffPreferenceStore());
        var pin = new PinService(
            cursors,
            index,
            fileFacts,
            decision ?? new FailClosedAgeCutDecision(),
            vision ?? new RecordingVisionIngest(),
            audio ?? new FakeAudioFileIngest(),
            opLog ?? new LocalOpLogWriter(),
            preferences);
        return (pin, cursors, index);
    }

    private static void SeedLongImage(FakeIngestCursorStore cursors, FakeMediaVectorIndex index, string path, string hash)
    {
        var longId = LongId(Modality.Image, hash, 0);
        cursors.FinishPath(path, hash, Path.GetDirectoryName(path) ?? path, "vis", [new ChunkWrite(longId, 0)]);
        cursors.UpsertMediaFile(path, Modality.Image);
        InsertLong(index, MediaCollections.Image, longId, path, 0);
    }

    private static void SeedShortImage(
        FakeIngestCursorStore cursors,
        FakeMediaVectorIndex index,
        string path,
        string hash,
        Modality modality = Modality.Image)
    {
        var longId = LongId(modality, hash, 0);
        var shortId = ShortId(modality, hash, 0);
        cursors.FinishPath(path, hash, Path.GetDirectoryName(path) ?? path, "vis", [new ChunkWrite(longId, 0)]);
        cursors.UpsertMediaFile(path, modality);
        cursors.SetMediaWidth(path, modality, MediaCollections.OldWidth);
        var collection = modality switch
        {
            Modality.Image => MediaCollections.ImageOld,
            Modality.Video => MediaCollections.VideoOld,
            Modality.Audio => MediaCollections.AudioOld,
            _ => MediaCollections.ImageOld,
        };
        InsertShort(index, collection, shortId, path);
    }

    private static void SeedLongAndShortImage(FakeIngestCursorStore cursors, FakeMediaVectorIndex index, string path, string hash)
    {
        var longId = LongId(Modality.Image, hash, 0);
        var shortId = ShortId(Modality.Image, hash, 0);
        cursors.FinishPath(path, hash, Path.GetDirectoryName(path) ?? path, "vis", [new ChunkWrite(longId, 0)]);
        cursors.UpsertMediaFile(path, Modality.Image);
        cursors.SetMediaWidth(path, Modality.Image, MediaCollections.OldWidth);
        InsertLong(index, MediaCollections.Image, longId, path, 0);
        InsertShort(index, MediaCollections.ImageOld, shortId, path);
    }

    private static void InsertLong(FakeMediaVectorIndex index, string collection, string contentId, string path, int ordinal)
    {
        var vector = UnitAxis((byte)(ordinal % 768));
        var insert = new MediaIndexInsert(
            contentId,
            path,
            Path.GetFileName(path),
            string.Empty,
            vector,
            "vis",
            true,
            Path.GetDirectoryName(path) ?? path,
            ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture),
            (ordinal + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        index.TryInsert(collection, insert);
    }

    private static void InsertShort(FakeMediaVectorIndex index, string collection, string contentId, string path)
    {
        var vector = new float[MediaCollections.OldWidth];
        vector[0] = 1f;
        var insert = new MediaIndexInsert(
            contentId,
            path,
            Path.GetFileName(path),
            string.Empty,
            vector,
            "vis",
            true,
            Path.GetDirectoryName(path) ?? path,
            string.Empty,
            string.Empty);
        index.TryInsert(collection, insert);
    }

    private static string LongId(Modality modality, string hash, int ordinal) =>
        ContentIdBuilder.Build(modality, hash, StoredWidth.W768, ordinal);

    private static string ShortId(Modality modality, string hash, int ordinal) =>
        ContentIdBuilder.Build(modality, hash, StoredWidth.W512, ordinal);

    private static float[] UnitAxis(int axis)
    {
        var vector = new float[768];
        vector[axis] = 1f;
        return vector;
    }

    private sealed class PassThroughAgeCutDecision : IAgeCutDecision
    {
        public bool TryPass(Modality modality)
        {
            if (modality == Modality.Text)
            {
                throw new RecallFaultException(RecallFault.AgeCutTextForbidden);
            }

            return true;
        }
    }

    private sealed class NeverCalledAgeCutDecision : IAgeCutDecision
    {
        public bool TryPass(Modality modality) =>
            throw new InvalidOperationException("TryPass should not be called.");
    }

    private sealed class ConfigurableMediaFileFacts : IMediaFileFacts
    {
        public bool ExistsValue { get; init; } = true;

        public DateTimeOffset? LastWriteUtcValue { get; init; } = DateTimeOffset.UtcNow;

        public bool Exists(string path) => ExistsValue;

        public DateTimeOffset? LastWriteUtc(string path) => LastWriteUtcValue;
    }

    private sealed class RecordingVisionIngest : IVisionFileIngest
    {
        public int Calls { get; private set; }

        public Task IndexImageAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }

        public Task IndexVideoFramesAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Video ingest is not used in these tests.");
    }

    private static void InsertLongZvec(ZVecMediaVectorIndex index, string collection, string contentId, string path)
    {
        var vector = UnitAxis(0);
        var insert = new MediaIndexInsert(
            contentId,
            path,
            Path.GetFileName(path),
            string.Empty,
            vector,
            "vis",
            true,
            Path.GetDirectoryName(path) ?? path,
            string.Empty,
            string.Empty);
        index.Create(collection, MediaCollections.Width);
        index.TryInsert(collection, insert).Should().BeTrue();
    }

    private sealed class RegrowingVideoIngest : IVisionFileIngest
    {
        private readonly FakeIngestCursorStore cursors;
        private readonly FakeMediaVectorIndex index;

        public RegrowingVideoIngest(FakeIngestCursorStore cursors, FakeMediaVectorIndex index)
        {
            this.cursors = cursors;
            this.index = index;
        }

        public Task IndexImageAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Image regrow is not used in these tests.");

        public Task IndexVideoFramesAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default)
        {
            var hash = cursors.GetFile(path)!.ContentHash;
            InsertLong(index, MediaCollections.Video, LongId(Modality.Video, hash, 0), path, 0);
            InsertLong(index, MediaCollections.Video, LongId(Modality.Video, hash, 1), path, 1);
            return Task.CompletedTask;
        }
    }

    private sealed class RegrowingVisionIngest : IVisionFileIngest
    {
        private readonly FakeIngestCursorStore cursors;
        private readonly FakeMediaVectorIndex index;

        public RegrowingVisionIngest(FakeIngestCursorStore cursors, FakeMediaVectorIndex index)
        {
            this.cursors = cursors;
            this.index = index;
        }

        public int Calls { get; private set; }

        public Task IndexImageAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default)
        {
            Calls++;
            var hash = cursors.GetFile(path)!.ContentHash;
            var longId = LongId(Modality.Image, hash, 0);
            InsertLong(index, MediaCollections.Image, longId, path, 0);
            return Task.CompletedTask;
        }

        public Task IndexVideoFramesAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Video regrow is not used in these tests.");
    }

    private sealed class TwentyHitTextIndex : ITextVectorIndex
    {
        public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default) =>
            Enumerable.Range(0, 20)
                .Select(i => new TextSearchHit(
                    $"text-{i}",
                    $@"D:\G\{i}.txt",
                    $"{i}.txt",
                    DateTimeOffset.UtcNow,
                    TextMatchReason.DenseAndFullText,
                    1 - i * 0.01,
                    i,
                    GrantPath: @"D:\G"))
                .ToList();

        public bool TryInsert(TextIndexInsert insert) => true;

        public bool TryDelete(string contentId) => true;

        public void Reconcile(IReadOnlyList<VectorLiveState> states)
        {
        }
    }

    private sealed class SingleHitTextIndex : ITextVectorIndex
    {
        private readonly string path;

        public SingleHitTextIndex(string path) => this.path = path;

        public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default) =>
        [
            new TextSearchHit(
                "text-id",
                path,
                Path.GetFileName(path),
                DateTimeOffset.UtcNow,
                TextMatchReason.DenseAndFullText,
                0.9,
                0,
                GrantPath: @"D:\G"),
        ];

        public bool TryInsert(TextIndexInsert insert) => true;

        public bool TryDelete(string contentId) => true;

        public void Reconcile(IReadOnlyList<VectorLiveState> states)
        {
        }
    }
}
