using FluentAssertions;
using Recall.Core.Age;
using Recall.Core.Download;
using Recall.Core.Faults;
using Recall.Core.Ingest;
using Recall.Core.Media;
using Recall.Core.Ocr;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Vocabulary;

namespace Recall.UnitTests;

public sealed class CloseOpenCoreBranchTests
{
    [Fact]
    public void OcrGoldenPages_SplitEqual_LastSliceHoldsRemainder()
    {
        var slices = OcrGoldenPages.SplitEqual("abcdefghij", 3);
        slices.Should().Equal("abc", "def", "ghij");
    }

    [Fact]
    public void OcrGoldenPages_CountBelowOne_Throws()
    {
        var act = () => OcrGoldenPages.SplitEqual("abc", 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void OcrGoldenPages_TextTooShort_Throws()
    {
        var act = () => OcrGoldenPages.SplitEqual("ab", 3);
        act.Should().Throw<ArgumentException>()
            .Which.Message.Should().Be("Source is shorter than the page count. (Parameter 'text')");
    }

    [Fact]
    public void OcrCharacterError_EmptyReference_WithHypothesis_IsFullError()
    {
        OcrCharacterErrorRate.Measure(string.Empty, "x").Rate.Should().Be(1d);
    }

    [Fact]
    public void OcrCharacterError_Identical_IsZero()
    {
        OcrCharacterErrorRate.Measure("abc", "abc").Rate.Should().Be(0d);
    }

    [Fact]
    public void VideoFrameFailure_Windows11_ReturnsNeedWindows11Key()
    {
        var key = VideoFrameFailureLabels.StatusKey(
            new NotSupportedException(FrameCadence.WindowsMinimumBuildMessage),
            emptyFrames: false);
        key.Should().Be(VideoFrameFailureLabels.NeedWindows11);
    }

    [Fact]
    public void VideoFrameFailure_EmptyFrames_ReturnsUnavailableKey()
    {
        VideoFrameFailureLabels.StatusKey(null, emptyFrames: true).Should().Be(VideoFrameFailureLabels.Unavailable);
    }

    [Fact]
    public void MediaPreviewSeek_NegativeStart_IsFalse()
    {
        MediaPreviewSeekPolicy.ShouldSeek(-1, TimeSpan.FromSeconds(10)).Should().BeFalse();
    }

    [Fact]
    public void MediaPreviewSeek_BeyondDuration_IsFalse()
    {
        MediaPreviewSeekPolicy.ShouldSeek(11, TimeSpan.FromSeconds(10)).Should().BeFalse();
    }

    [Fact]
    public void MediaPreviewSeek_ZeroDuration_IsFalse()
    {
        MediaPreviewSeekPolicy.ShouldSeek(4, TimeSpan.Zero).Should().BeFalse();
    }

    [Fact]
    public void MediaPreviewSeek_InRange_IsTrue()
    {
        MediaPreviewSeekPolicy.ShouldSeek(4, TimeSpan.FromSeconds(10)).Should().BeTrue();
    }

    [Fact]
    public void ModelDownloadRowKind_Video_Throws()
    {
        var act = () => ModelDownloadRowKindExtensions.FromModality(Modality.Video);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AgeCutCorpus_Text_Throws()
    {
        var act = () => AgeCutCorpus.TryLoad(Modality.Text, out _);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.AgeCutTextForbidden);
    }

    [Fact]
    public void OcrGoldenPages_Null_Throws()
    {
        var act = () => OcrGoldenPages.SplitEqual(null!, 2);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VideoFrameFailure_OtherError_ReturnsNull()
    {
        VideoFrameFailureLabels.StatusKey(new InvalidOperationException("decode"), emptyFrames: false).Should().BeNull();
    }

    [Fact]
    public void IngestKindLabels_Text_IsTextKey() =>
        IngestKindLabels.Key(GrantFileKinds.Text).Should().Be("Ingest_Text");

    [Fact]
    public void IngestKindLabels_Code_IsCodeKey() =>
        IngestKindLabels.Key(GrantFileKinds.Code).Should().Be("Ingest_Code");

    [Fact]
    public void IngestKindLabels_Video_IsVideoKey() =>
        IngestKindLabels.Key(GrantFileKinds.Video).Should().Be("Ingest_Video");

    [Fact]
    public void FailClosedAgeCut_Image_IsFalse() =>
        new FailClosedAgeCutDecision().TryPass(Modality.Image).Should().BeFalse();

    [Fact]
    public void FailClosedAgeCut_Text_Throws()
    {
        var act = () => new FailClosedAgeCutDecision().TryPass(Modality.Text);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.AgeCutTextForbidden);
    }

    [Fact]
    public void WindowIngestPolicy_Verifying_IsFalse() =>
        WindowIngestPolicy.ShouldStart(TextModelDownloadPhase.Verifying).Should().BeFalse();

    [Fact]
    public void FtsQueryString_Colon_BecomesSpace() =>
        FtsQueryString.ForZVec("Fahrrad: Platten").Should().Be("Fahrrad  Platten");

    [Fact]
    public void OcrCharacterErrorRate_MeasureGolden_ShapesArabicDigits()
    {
        var cer = OcrCharacterErrorRate.MeasureGolden("رقم ٢٠٣٧", "رقم 2037");
        cer.Rate.Should().Be(0d);
    }
}
