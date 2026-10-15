using System.Text.RegularExpressions;
using FluentAssertions;
using Recall.Core.Search;
using Recall.UI.Pages;

namespace Recall.UnitTests;

public sealed class TextFindSessionTests
{
    [Fact]
    public void ShouldQuery_Blank_IsFalse() =>
        TextFindSession.ShouldQuery(" ").Should().BeFalse();

    [Fact]
    public void ShouldQuery_Sentence_IsTrue() =>
        TextFindSession.ShouldQuery("mars").Should().BeTrue();

    [Fact]
    public void FaultKey_Null_IsNull() =>
        TextFindSession.FaultKey(null).Should().BeNull();

    [Fact]
    public void FaultKey_Cancelled_IsSearchFault() =>
        TextFindSession.FaultKey(new OperationCanceledException()).Should().Be("Search_Fault");

    [Fact]
    public void FaultKey_Other_IsSearchFault() =>
        TextFindSession.FaultKey(new InvalidOperationException()).Should().Be("Search_Fault");

    [Fact]
    public void Build_Empty_HasNoCards()
    {
        var view = TextFindSession.Build([], _ => true);
        view.HasHits.Should().BeFalse();
        view.Immediate.Should().BeEmpty();
        view.Deferred.Should().BeEmpty();
    }

    [Fact]
    public void Build_SixHits_FirstFiveAreImmediate()
    {
        var hits = Enumerable.Range(0, 6).Select(i => new TextSearchHit(
            "id-" + i,
            "/notes/note-" + i + ".txt",
            "note-" + i + ".txt",
            DateTimeOffset.UtcNow,
            TextMatchReason.DenseAndFullText,
            1.0)).ToList();
        var view = TextFindSession.Build(hits, _ => true);
        view.Immediate.Should().HaveCount(5);
        view.Deferred.Should().HaveCount(1);
        view.HasHits.Should().BeTrue();
    }

    [Fact]
    public void Build_MissingFile_StaysUnavailable()
    {
        var hits = new List<TextSearchHit>
        {
            new("id-1", "/notes/a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1.0),
        };
        var view = TextFindSession.Build(hits, _ => false);
        view.Immediate.Should().HaveCount(1);
        view.Immediate[0].Unavailable.Should().BeTrue();
        view.Immediate[0].KindKey.Should().Be("Kind_Document");
        view.Immediate[0].ReasonKey.Should().Be("Search_Match_MeaningAndWords");
    }

    [Fact]
    public void Build_LocalTime_UsesInvariantDigits()
    {
        var hits = new List<TextSearchHit>
        {
            new("id-1", "/notes/a.txt", "a.txt", new DateTimeOffset(2024, 7, 14, 16, 42, 0, TimeSpan.Zero), TextMatchReason.DenseAndFullText, 1.0),
        };
        var view = TextFindSession.Build(hits, _ => true);
        var localTime = view.Immediate[0].LocalTime;
        Regex.IsMatch(localTime, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}$").Should().BeTrue();
        localTime.Should().NotContainAny("\u0660", "\u0661", "\u0662", "\u0663", "\u0664", "\u0665", "\u0666", "\u0667", "\u0668", "\u0669");
    }

    [Fact]
    public void MarkUnavailable_OnlyMissing()
    {
        TextFindSession.MarkUnavailable(LocalFileOpenResult.Missing).Should().BeTrue();
        TextFindSession.MarkUnavailable(LocalFileOpenResult.Opened).Should().BeFalse();
        TextFindSession.MarkUnavailable(LocalFileOpenResult.Failed).Should().BeFalse();
    }

    [Fact]
    public void OpenFaultKey_OnlyFailed()
    {
        TextFindSession.OpenFaultKey(LocalFileOpenResult.Failed).Should().Be("Search_OpenFailed");
        TextFindSession.OpenFaultKey(LocalFileOpenResult.Opened).Should().BeNull();
        TextFindSession.OpenFaultKey(LocalFileOpenResult.Missing).Should().BeNull();
    }
}
