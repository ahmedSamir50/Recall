using FluentAssertions;
using Recall.Core.Search;

namespace Recall.UnitTests;

public sealed class LocalFileOpenPolicyTests
{
    [Fact]
    public void Classify_Whitespace_IsEmpty() =>
        LocalFileOpenPolicy.Classify(" ").Should().Be(LocalFileProbe.Empty);

    [Fact]
    public void Classify_ContentScheme_IsContentUri() =>
        LocalFileOpenPolicy.Classify("content://tree/1").Should().Be(LocalFileProbe.ContentUri);

    [Fact]
    public void Classify_ContentScheme_IgnoresCase() =>
        LocalFileOpenPolicy.Classify("CONTENT://tree/1").Should().Be(LocalFileProbe.ContentUri);

    [Fact]
    public void Classify_DiskPath_IsFilesystem() =>
        LocalFileOpenPolicy.Classify(@"D:\notes\a.txt").Should().Be(LocalFileProbe.Filesystem);

    [Fact]
    public void Decide_Thrown_IsFailed() =>
        LocalFileOpenPolicy.Decide(LocalFileProbe.Filesystem, true, true, false, threw: true).Should().Be(LocalFileOpenResult.Failed);

    [Fact]
    public void Decide_Empty_IsFailed() =>
        LocalFileOpenPolicy.Decide(LocalFileProbe.Empty, false, false, false, false).Should().Be(LocalFileOpenResult.Failed);

    [Fact]
    public void Decide_ContentAccepted_IsOpened() =>
        LocalFileOpenPolicy.Decide(LocalFileProbe.ContentUri, false, false, true, false).Should().Be(LocalFileOpenResult.Opened);

    [Fact]
    public void Decide_ContentRejected_IsFailed() =>
        LocalFileOpenPolicy.Decide(LocalFileProbe.ContentUri, false, false, false, false).Should().Be(LocalFileOpenResult.Failed);

    [Fact]
    public void Decide_MissingFile_IsMissing() =>
        LocalFileOpenPolicy.Decide(LocalFileProbe.Filesystem, false, false, false, false).Should().Be(LocalFileOpenResult.Missing);

    [Fact]
    public void Decide_LauncherOpened_IsOpened() =>
        LocalFileOpenPolicy.Decide(LocalFileProbe.Filesystem, true, true, false, false).Should().Be(LocalFileOpenResult.Opened);

    [Fact]
    public void Decide_LauncherRejected_IsFailed() =>
        LocalFileOpenPolicy.Decide(LocalFileProbe.Filesystem, true, false, false, false).Should().Be(LocalFileOpenResult.Failed);

    [Fact]
    public void Exists_Empty_IsFalse() =>
        LocalFileOpenPolicy.Exists(LocalFileProbe.Empty, true, true).Should().BeFalse();

    [Fact]
    public void Exists_ContentStream_IsTrue() =>
        LocalFileOpenPolicy.Exists(LocalFileProbe.ContentUri, false, true).Should().BeTrue();

    [Fact]
    public void Exists_ContentStream_IsFalse() =>
        LocalFileOpenPolicy.Exists(LocalFileProbe.ContentUri, true, false).Should().BeFalse();

    [Fact]
    public void Exists_Filesystem_MatchesDisk()
    {
        LocalFileOpenPolicy.Exists(LocalFileProbe.Filesystem, true, false).Should().BeTrue();
        LocalFileOpenPolicy.Exists(LocalFileProbe.Filesystem, false, true).Should().BeFalse();
    }
}
