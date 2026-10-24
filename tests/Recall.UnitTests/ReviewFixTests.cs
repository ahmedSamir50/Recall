using FluentAssertions;
using Recall.Core.Download;
using Recall.Core.Faults;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.Core.Vectors;

namespace Recall.UnitTests;

public sealed class ReviewFixTests
{
    [Theory]
    [InlineData(206, 10, DownloadBodyPlacement.Append)]
    [InlineData(200, 10, DownloadBodyPlacement.Replace)]
    [InlineData(416, 10, DownloadBodyPlacement.UseExistingPartial)]
    [InlineData(206, 0, DownloadBodyPlacement.Replace)]
    public void DownloadTransfer_PlacesBody(int status, long existing, DownloadBodyPlacement placement)
    {
        DownloadTransfer.Place(status, existing).Should().Be(placement);
    }

    [Fact]
    public void DownloadTransfer_PartialContentLengthIsRemainder()
    {
        DownloadTransfer.ExpectedTotalBytes(206, 100, 50).Should().Be(150);
        DownloadTransfer.ExpectedTotalBytes(200, 100, 80).Should().Be(80);
        DownloadTransfer.ExpectedTotalBytes(416, 40, null).Should().Be(40);
    }

    [Fact]
    public void DownloadTransfer_ShortReadIsNotComplete_UnknownLengthEofIsComplete()
    {
        DownloadTransfer.ReceivedFullBody(10, 20).Should().BeFalse();
        DownloadTransfer.ReceivedFullBody(20, 20).Should().BeTrue();
        DownloadTransfer.ReceivedFullBody(8, null).Should().BeTrue();
        DownloadTransfer.ReceivedFullBody(0, null).Should().BeFalse();
    }

    [Fact]
    public void EmptyTitle_DoesNotEmbedTheWordNone()
    {
        var wrapped = EmbeddingTaskPrefixes.WrapTitledDocument("  ", "hello");
        wrapped.Should().Be("text: hello");
        wrapped.Should().NotContain("none");
    }

    [Fact]
    public void LongNote_EachChunkKeepsTitlePrefix_AndDoesNotSplitSurrogate()
    {
        var emoji = "\U0001F600";
        var pattern = new string('a', 30) + emoji + new string('b', 30) + "\n";
        var content = string.Concat(Enumerable.Repeat(pattern, 50));
        var ends = Enumerable.Range(0, content.Length + 1).ToList();
        var pieces = DocumentPatchPlanner.Plan("Notes", content, null, 0, ends);
        pieces.Count.Should().BeGreaterThan(1);
        foreach (var piece in pieces)
        {
            EmbeddingTaskPrefixes.WrapTitledDocument("Notes", piece.Raw).Should().StartWith("title: Notes | text: ");
            HasLoneSurrogate(piece.Raw).Should().BeFalse();
            content.Contains(piece.Raw).Should().BeTrue();
        }

        pieces[0].CharStart.Should().Be(0);
        pieces[^1].Raw[^30..].Should().Be(content[^30..]);
        pieces[^1].Raw.Should().Contain(emoji);
    }

    [Fact]
    public void MeanPool_AveragesTwoIdenticalRows()
    {
        var row = new float[MatryoshkaVectorMath.NativeWidth];
        row[0] = 4f;
        var rows = new float[row.Length * 2];
        row.CopyTo(rows, 0);
        row.CopyTo(rows, row.Length);
        var pooled = TokenMeanPool.Mean(rows, MatryoshkaVectorMath.NativeWidth);
        pooled[0].Should().Be(4f);
        pooled.Length.Should().Be(MatryoshkaVectorMath.NativeWidth);
    }

    [Fact]
    public void IsInsideGrant_DoesNotMatchSiblingPrefix()
    {
        ScopeGrantPolicy.IsInsideGrant(@"D:\Photos", @"D:\PhotosExtra\a.txt").Should().BeFalse();
        ScopeGrantPolicy.IsInsideGrant(@"D:\Photos", @"D:\Photos\a.txt").Should().BeTrue();
        ScopeGrantPolicy.IsInsideGrant(@"D:\Photos", @"D:\Photos").Should().BeTrue();
    }

    [Fact]
    public void HybridFusion_Requires60()
    {
        HybridFusion.RrfRankConstant.Should().Be(60);
        var act = () => HybridFusion.RequireRankConstant(61);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.RrfRankConstant);
    }

    [Fact]
    public void AgeCutoff_AndEmptyGrantKinds_UseFaultCodes()
    {
        var age = new AgeCutoffPreferenceService(new MemoryAgeStore());
        var ageAct = () => age.Set((Recall.Core.Vocabulary.AgeCutoffYears)3);
        ageAct.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.AgeCutoffNotAllowed);

        var grants = new ScopeGrantService(new MemoryGrantStore());
        grants.TryAddGrant(new ScopeFolderGrant(@"D:\Notes", "Notes"));
        var kindsAct = () => grants.SetGrantFileKinds(@"D:\Notes", GrantFileKinds.None);
        kindsAct.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.GrantFileKindsEmpty);
    }

    private static bool HasLoneSurrogate(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1])))
            {
                return true;
            }

            if (char.IsLowSurrogate(text[i]) && (i == 0 || !char.IsHighSurrogate(text[i - 1])))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class MemoryAgeStore : Recall.Core.Ports.IAgeCutoffPreferenceStore
    {
        public Recall.Core.Vocabulary.AgeCutoffYears Get() => Recall.Core.Vocabulary.AgeCutoffYears.Five;

        public void Set(Recall.Core.Vocabulary.AgeCutoffYears years)
        {
        }
    }

    private sealed class MemoryGrantStore : Recall.Core.Ports.IScopeGrantStore
    {
        private readonly List<ScopeFolderGrant> stored = [];

        public IReadOnlyList<ScopeFolderGrant> List() => stored;

        public void Save(IReadOnlyList<ScopeFolderGrant> grants)
        {
            stored.Clear();
            stored.AddRange(grants);
        }
    }
}
