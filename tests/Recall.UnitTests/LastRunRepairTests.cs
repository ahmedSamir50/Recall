using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Faults;
using Recall.Core.Ingest;
using Recall.Core.Preferences;
using Recall.Core.Runtime;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.UI.Pages;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class LastRunRepairTests
{
    private static string NewDatabase() =>
        Path.Combine(Path.GetTempPath(), "recall-last-run-" + Guid.NewGuid().ToString("N") + ".db");

    [Fact]
    public void Decide_FinishedSameHash_IsSkip() =>
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Finished, "g", ""), "h1", false)
            .Should().Be(IngestCursorDecision.Skip);

    [Fact]
    public void Decide_ReadingSameHashWithChunks_IsSkip() =>
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Reading, "g", ""), "h1", true)
            .Should().Be(IngestCursorDecision.Skip);

    [Fact]
    public void Decide_ReadingSameHashNoChunks_IsRead() =>
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Reading, "g", ""), "h1", false)
            .Should().Be(IngestCursorDecision.Read);

    [Fact]
    public void Decide_BlankHash_ThrowsArgumentException()
    {
        Action act = () => IngestCursorPolicy.Decide(null, "  ", false);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Leftover_Chunks_Finishes() =>
        LeftoverPolicy.Decide(true, true).Should().Be(LeftoverAction.Finish);

    [Fact]
    public void Leftover_MissingFile_IsMissing() =>
        LeftoverPolicy.Decide(false, false).Should().Be(LeftoverAction.Missing);

    [Fact]
    public void Leftover_PresentWithoutChunks_Pauses() =>
        LeftoverPolicy.Decide(false, true).Should().Be(LeftoverAction.Pause);

    [Fact]
    public void PauseOnlyReading_Finished_StaysFinished() =>
        IngestCancellation.PauseOnlyReading(IngestCursorState.Finished).Should().Be(IngestCursorState.Finished);

    [Fact]
    public void PauseOnlyReading_Reading_BecomesPaused() =>
        IngestCancellation.PauseOnlyReading(IngestCursorState.Reading).Should().Be(IngestCursorState.Paused);

    [Fact]
    public void MarkStopped_WritesPaused()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        store.MarkReading("/g/a", "hash", "/g");
        store.MarkStopped("/g/a", IngestStopKeys.MemoryBudget);
        var row = store.GetFile("/g/a") ?? throw new InvalidOperationException("row");
        row.State.Should().Be(IngestCursorState.Paused);
    }

    [Fact]
    public void MarkStopped_FinishedRow_StaysFinished()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        store.FinishPath("/g/a", "hash", "/g", "enc", [new ChunkWrite("id0", 0)]);
        store.MarkStopped("/g/a", IngestStopKeys.MemoryBudget);
        var row = store.GetFile("/g/a") ?? throw new InvalidOperationException("row");
        row.State.Should().Be(IngestCursorState.Finished);
    }

    [Fact]
    public void FinishExisting_NoChunks_ThrowsInvalidOperationException()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        store.MarkReading("/g/a", "hash", "/g");
        Action act = () => store.FinishExisting("/g/a");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsSeen_IncludesPaused() =>
        IngestKindCountPolicy.IsSeen(IngestCursorState.Paused).Should().BeTrue();

    [Fact]
    public void IsFinished_Paused_IsFalse() =>
        IngestKindCountPolicy.IsFinished(IngestCursorState.Paused).Should().BeFalse();

    [Fact]
    public void Percent_TwoFinishedOfTwo_Stays100AfterAnotherFilePauses()
    {
        var store = new FakeIngestCursorStore();
        store.MarkFinished("/g/a.txt", "h1", "/g", "e");
        store.MarkFinished("/g/b.txt", "h2", "/g", "e");
        store.MarkReading("/g/c.txt", "h3", "/g");
        store.MarkStopped("/g/c.txt", IngestStopKeys.Interrupted);
        var count = store.Count(GrantFileKinds.Text);
        count.Finished.Should().Be(2);
        count.Seen.Should().Be(3);
        count.Percent.Should().Be(66);
    }

    [Fact]
    public void Gpu_Null_DoesNotBlock() => GpuCooldownPolicy.Blocks(null).Should().BeFalse();

    [Fact]
    public void Gpu_80_DoesNotBlock() => GpuCooldownPolicy.Blocks(80).Should().BeFalse();

    [Fact]
    public void Gpu_81_Blocks() => GpuCooldownPolicy.Blocks(81).Should().BeTrue();

    [Fact]
    public void Retry_FiveMinutes_IsNext()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IngestRetryClock.Next(start, 5).Should().Be(start.AddMinutes(5));
    }

    [Fact]
    public void ShouldRun_BeforeNext_IsFalse()
    {
        var next = DateTimeOffset.UtcNow.AddMinutes(5);
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, next, true, false, true).Should().BeFalse();
    }

    [Fact]
    public void ShouldRun_GateClosed_IsFalse() =>
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-1), false, false, true).Should().BeFalse();

    [Fact]
    public void ShouldRun_GpuHot_IsFalse() =>
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-1), true, true, true).Should().BeFalse();

    [Fact]
    public void ShouldRun_ModelNotReady_IsFalse() =>
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-1), true, false, false).Should().BeFalse();

    [Fact]
    public void ShouldRun_OpenAndCool_IsTrue() =>
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-1), true, false, true).Should().BeTrue();

    [Fact]
    public void Defer_AddsTheSameMinutes()
    {
        var now = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
        IngestRetryClock.Defer(now, 5).Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void AllowAutomatic_ThirdFailure_IsFalse() =>
        IngestRetryPolicy.AllowAutomatic(3).Should().BeFalse();

    [Fact]
    public void AllowAutomatic_Second_IsTrue() =>
        IngestRetryPolicy.AllowAutomatic(2).Should().BeTrue();

    [Fact]
    public void RetryDelay_0_Throws()
    {
        var service = new RetryDelayPreferenceService(new MemoryRetryDelayPreferenceStore());
        Action act = () => service.Set(0);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.RetryDelayNotAllowed);
    }

    [Fact]
    public void RetryDelay_61_Throws()
    {
        var service = new RetryDelayPreferenceService(new MemoryRetryDelayPreferenceStore());
        Action act = () => service.Set(61);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.RetryDelayNotAllowed);
    }

    [Fact]
    public void RetryDelay_5_Stores5()
    {
        var store = new MemoryRetryDelayPreferenceStore();
        var service = new RetryDelayPreferenceService(store);
        service.Set(5);
        store.Get().Should().Be(5);
    }

    [Fact]
    public void RetryDelay_60_Stores60()
    {
        var store = new MemoryRetryDelayPreferenceStore();
        var service = new RetryDelayPreferenceService(store);
        service.Set(60);
        store.Get().Should().Be(60);
    }

    [Fact]
    public void Promote_AbsentMatchingHash_InsertsPathAtZero()
    {
        var hash = "abc";
        var rows = new[] { new IngestFileRow("/g/pic.jpg", hash, IngestCursorState.Finished, "/g", "enc") };
        var hits = new List<MediaSearchHit> { new("x", "/g/other.jpg", 0, 0, 0.5, 0.5) };
        var promoted = PictureExactFile.Promote(hits, hash, rows);
        promoted[0].SourcePath.Should().Be("/g/pic.jpg");
        promoted[0].Similarity.Should().Be(1);
    }

    [Fact]
    public void Promote_NoMatch_SameOrder()
    {
        var hits = new List<MediaSearchHit>
        {
            new("a", "/g/a.jpg", 0, 0, 0.5, 0.5),
            new("b", "/g/b.jpg", 0, 0, 0.4, 0.4),
        };
        PictureExactFile.Promote(hits, "missing", []).Select(h => h.SourcePath)
            .Should().Equal("/g/a.jpg", "/g/b.jpg");
    }

    [Fact]
    public void Promote_AlreadyFirst_StaysFirst()
    {
        var hash = "abc";
        var rows = new[] { new IngestFileRow("/g/pic.jpg", hash, IngestCursorState.Finished, "/g", "enc") };
        var hits = new List<MediaSearchHit>
        {
            new("a", "/g/pic.jpg", 0, 0, 0.9, 0.9),
            new("b", "/g/b.jpg", 0, 0, 0.4, 0.4),
        };
        PictureExactFile.Promote(hits, hash, rows).Select(h => h.SourcePath)
            .Should().Equal("/g/pic.jpg", "/g/b.jpg");
    }

    [Fact]
    public void Marks_ExactToken_ReturnsSpan()
    {
        var marks = Bm25Highlight.Marks("hello world", "world");
        marks.Should().ContainSingle();
        marks[0].Start.Should().Be(6);
        marks[0].Length.Should().Be(5);
    }

    [Fact]
    public void Marks_NoToken_Empty() => Bm25Highlight.Marks("hello", "zzz").Should().BeEmpty();

    [Fact]
    public void Marks_EmptyQuery_Empty() => Bm25Highlight.Marks("hello", "").Should().BeEmpty();

    [Fact]
    public void MatchStrength_One_Is100() => MatchStrength.Percent(1).Should().Be(100);

    [Fact]
    public void MatchStrength_Half_Is50() => MatchStrength.Percent(0.5).Should().Be(50);

    [Fact]
    public void MatchStrength_AboveOne_IsNull() => MatchStrength.Percent(1.1).Should().BeNull();

    [Fact]
    public void MatchStrength_Negative_IsNull() => MatchStrength.Percent(-0.1).Should().BeNull();

    [Fact]
    public void Split_OmitsEmptyVideo()
    {
        var cards = new List<TextFindCard> { new("/a.txt", "a.txt", "Search_Kind_Text", "", "Search_Match_Words", false) };
        SearchGroups.Split(cards).Select(g => g.GroupKey).Should().NotContain("Search_Group_Video");
    }

    [Fact]
    public void Split_CodeStaysInText()
    {
        var cards = new List<TextFindCard> { new("/a.cs", "a.cs", "Search_Kind_Code", "", "Search_Match_Words", false) };
        SearchGroups.Split(cards)[0].GroupKey.Should().Be("Search_Group_Text");
    }

    [Fact]
    public void Split_OrderIsTextImageVideoAudio()
    {
        var cards = new List<TextFindCard>
        {
            new("/a.mp3", "a.mp3", "Search_Kind_Audio", "", "", false),
            new("/a.jpg", "a.jpg", "Search_Kind_Image", "", "", false),
            new("/a.txt", "a.txt", "Search_Kind_Text", "", "Search_Match_Words", false),
            new("/a.mp4", "a.mp4", "Search_Kind_Video", "", "", false),
        };
        SearchGroups.Split(cards).Select(g => g.GroupKey).Should().ContainInOrder(
            "Search_Group_Text", "Search_Group_Image", "Search_Group_Video", "Search_Group_Audio");
    }

    [Fact]
    public void Build_ImagePath_KindIsImage()
    {
        var hits = new List<TextSearchHit>
        {
            new("id", "/g/pic.jpg", "pic.jpg", DateTimeOffset.UtcNow, TextMatchReason.FullText, 0.8),
        };
        TextFindSession.Build(hits, _ => true).Cards[0].KindKey.Should().Be("Search_Kind_Image");
    }

    [Fact]
    public void Build_VideoWords_ReasonIsWordsOnMedia()
    {
        var hits = new List<TextSearchHit>
        {
            new("id", "/g/v.mp4", "v.mp4", DateTimeOffset.UtcNow, TextMatchReason.WordsOnMedia, 0.5),
        };
        TextFindSession.Build(hits, _ => true).Cards[0].ReasonKey.Should().Be("Search_Match_WordsOnMedia");
    }

    [Fact]
    public void Build_FullText_IsNotMeaningAndWords()
    {
        var hits = new List<TextSearchHit>
        {
            new("id", "/g/a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.FullText, 0.5),
        };
        TextFindSession.Build(hits, _ => true).Cards[0].ReasonKey.Should().Be("Search_Match_Words");
    }

    [Fact]
    public void Build_UnixEpoch_TimeIsEmpty()
    {
        var hits = new List<TextSearchHit>
        {
            new("id", "/g/a.txt", "a.txt", DateTimeOffset.UnixEpoch, TextMatchReason.FullText, 0.5),
        };
        TextFindSession.Build(hits, _ => true).Cards[0].LocalTime.Should().BeEmpty();
    }

    [Fact]
    public void ResultAction_Image_IsView() => ResultActionPolicy.Decide("/a.jpg").Should().Be(ResultActionKind.View);

    [Fact]
    public void ResultAction_Video_IsPlay() => ResultActionPolicy.Decide("/a.mp4").Should().Be(ResultActionKind.Play);

    [Fact]
    public void ResultAction_Text_IsOpenOnly() => ResultActionPolicy.Decide("/a.txt").Should().Be(ResultActionKind.OpenOnly);

    [Fact]
    public void HomeRazor_HidesEmptyTitleWhenResultsExist()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.UI", "Pages", "Find", "Home.razor");
        var text = File.ReadAllText(path);
        text.Should().Contain("ShowEmptyPrompt");
        text.Should().NotContain("@if (!TextFindSession.ShouldQuery(query) || !hasSearched)");
    }

    [Fact]
    public void HomeRazor_DoesNotCallPreviewPlay()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.UI", "Pages", "Find", "Home.razor.cs");
        File.ReadAllText(path).Should().NotContain("Preview.PlayAsync");
    }

    [Fact]
    public void HomeRazor_PicturePickStartsFind()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.UI", "Pages", "Find", "Home.razor.cs");
        var text = File.ReadAllText(path);
        text.Should().Contain("RunPictureSearchAsync");
        text.Should().Contain("OnPictureSelectedAsync");
    }

    [Fact]
    public void OpenAtAsync_DoesNotCallPlay()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.Maui", "Platform", "Files", "MauiLocalFileOpener.cs");
        File.ReadAllText(path).Should().NotContain("PlayAsync");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Recall.slnx was not found above the test output directory.");
    }
}