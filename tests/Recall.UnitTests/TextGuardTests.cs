using System.Text;
using FluentAssertions;
using Recall.Core.Sync;
using Recall.Core.Text;

namespace Recall.UnitTests;

public sealed class TextGuardTests
{
    [Fact]
    public void DefaultExtensions_DoNotIncludeLog()
    {
        TextIngestOptions.DefaultExtensions.Should().NotContain(".log");
        GrantTextFiles.IsTextFile("notes.txt").Should().BeTrue();
        GrantTextFiles.IsTextFile("trace.log").Should().BeFalse();
    }

    [Fact]
    public void Signature_RejectsExecutableAndScript_AcceptsNote()
    {
        TextFileSignature.IsSafeText(Encoding.UTF8.GetBytes("hello")).Should().BeTrue();
        TextFileSignature.IsSafeText([(byte)'M', (byte)'Z', 0x90, 0]).Should().BeFalse();
        TextFileSignature.IsSafeText(Encoding.UTF8.GetBytes("#!/bin/sh\necho hi")).Should().BeFalse();
        TextFileSignature.IsSafeText(Encoding.UTF8.GetBytes("#requires -Version 5\n")).Should().BeFalse();
    }

    [Fact]
    public void OpLog_WritesOneFileForTheUtcDay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "recall-oplog-" + Guid.NewGuid().ToString("N"));
        var writer = new LocalOpLogWriter(directory);
        writer.AppendInsert("content-1");
        var files = Directory.GetFiles(directory, "*.log");
        files.Should().ContainSingle();
        Path.GetFileName(files[0]).Should().Be(DateTime.UtcNow.ToString(LocalOpLogWriter.DayFilePattern) + ".log");
        File.ReadAllText(files[0]).Should().Contain("content-1");
        Directory.Delete(directory, true);
    }
}
