using FluentAssertions;
using Recall.Core.Download;
using Recall.Core.Faults;
using Recall.Core.Indexing;
using Recall.Core.Text;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;

namespace Recall.UnitTests;

public sealed class TextPipelineTests
{
    [Fact]
    public void WrapSearchQuery_SecondWrap_Throws()
    {
        var once = EmbeddingTaskPrefixes.WrapSearchQuery("mars");
        var act = () => EmbeddingTaskPrefixes.WrapSearchQuery(once);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.SearchPrefixAppliedTwice);
    }

    [Fact]
    public void WrapTitledDocument_FormatsOnce()
    {
        var doc = EmbeddingTaskPrefixes.WrapTitledDocument("Notes", "hello");
        doc.Should().Be("title: Notes | text: hello");
    }

    [Fact]
    public void ContentId_ChangesWhenChunkOrdinalChanges()
    {
        var hash = ContentIdBuilder.HashSourceContent("same");
        var first = ContentIdBuilder.Build(Modality.Text, hash, StoredWidth.W512, 0);
        var second = ContentIdBuilder.Build(Modality.Text, hash, StoredWidth.W512, 1);
        first.Should().NotBe(second);
    }

    [Fact]
    public void ContentId_DoesNotChangeWhenDeviceFieldsDiffer()
    {
        var hash = ContentIdBuilder.HashSourceContent("note");
        var a = ContentIdBuilder.Build(Modality.Text, hash, StoredWidth.W512, 0);
        var b = ContentIdBuilder.Build(Modality.Text, hash, StoredWidth.W512, 0);
        a.Should().Be(b);
    }

    [Fact]
    public void IndexPolicy_Text512_Accepts_128Throws()
    {
        var policy = new RecallIndexPolicy();
        policy.TextStoredWidth.Should().Be(StoredWidth.W512);
        var act128 = () => policy.EnsureStorableWidth(Modality.Text, RecallIndexPolicy.BlockedWidth);
        act128.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.StoredWidthBlocked);
        var act256 = () => policy.EnsureStorableWidth(Modality.Text, 256);
        act256.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.TextWidthNot512);
    }

    [Fact]
    public void Matryoshka_AfterCut_LengthIsOne()
    {
        var full = new float[768];
        for (var i = 0; i < full.Length; i++)
        {
            full[i] = i + 1;
        }

        MatryoshkaVectorMath.L2Normalize(full);
        MatryoshkaVectorMath.L2Length(full).Should().BeApproximately(1d, 1e-5);
        var cut = MatryoshkaVectorMath.CutAndNormalize(full, 512);
        MatryoshkaVectorMath.L2Length(cut).Should().BeApproximately(1d, 1e-5);
    }

    [Fact]
    public void DownloadRequestBuilder_OmitsForbiddenPayload()
    {
        ModelWeightDownloadRequestBuilder.ContainsForbiddenPayload(
            documentName: "secret.txt",
            documentText: "hello",
            vector: new float[4],
            deviceId: "id",
            nativeDeviceName: "native",
            customName: "custom").Should().BeTrue();

        ModelWeightDownloadRequestBuilder.ContainsForbiddenPayload(
            null,
            null,
            null,
            null,
            null,
            null).Should().BeFalse();
    }
}
