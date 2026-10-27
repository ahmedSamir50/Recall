using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Resources;
using Recall.Core.Scope;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class CloseOpenIngestUiTests
{
    private static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void Count_Text_TwoOfFive_IsForty()
    {
        var store = new FakeIngestCursorStore();
        store.MarkFinished(@"C:\grant\a.txt", "h1", @"C:\grant", "enc");
        store.MarkFinished(@"C:\grant\b.txt", "h2", @"C:\grant", "enc");
        store.MarkReading(@"C:\grant\c.txt", "h3", @"C:\grant");
        store.MarkReading(@"C:\grant\d.txt", "h4", @"C:\grant");
        store.MarkReading(@"C:\grant\e.txt", "h5", @"C:\grant");

        var count = store.Count(GrantFileKinds.Text);
        count.Finished.Should().Be(2);
        count.Seen.Should().Be(5);
        count.Percent.Should().Be(40);
    }

    [Fact]
    public void Count_Code_IsOneOfOne()
    {
        var store = new FakeIngestCursorStore();
        store.MarkFinished(@"C:\grant\Main.cs", "h1", @"C:\grant", "enc");

        var count = store.Count(GrantFileKinds.Code);
        count.Finished.Should().Be(1);
        count.Seen.Should().Be(1);
        count.Percent.Should().Be(100);
    }

    [Fact]
    public void Count_Image_Reading_IsZeroPercent()
    {
        var store = new FakeIngestCursorStore();
        store.MarkReading(@"C:\grant\photo.jpg", "h1", @"C:\grant");

        var count = store.Count(GrantFileKinds.Image);
        count.Finished.Should().Be(0);
        count.Seen.Should().Be(1);
        count.Percent.Should().Be(0);
    }

    [Fact]
    public void Count_Empty_IsZero()
    {
        var store = new FakeIngestCursorStore();
        var count = store.Count(GrantFileKinds.Text);
        count.Finished.Should().Be(0);
        count.Seen.Should().Be(0);
        count.Percent.Should().Be(0);
    }

    [Fact]
    public void Count_CombinedKind_Throws()
    {
        var store = new FakeIngestCursorStore();
        var act = () => store.Count(GrantFileKinds.All);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void List_TextAndImage_OmitsAudio()
    {
        var store = new FakeIngestCursorStore();
        var service = new IngestStatusService(store);
        var list = service.List(GrantFileKinds.Text | GrantFileKinds.Image);
        list.Should().HaveCount(2);
        list[0].Kind.Should().Be(GrantFileKinds.Text);
        list[1].Kind.Should().Be(GrantFileKinds.Image);
    }

    [Fact]
    public void List_None_IsEmpty()
    {
        var service = new IngestStatusService(new FakeIngestCursorStore());
        service.List(GrantFileKinds.None).Should().BeEmpty();
    }

    [Fact]
    public void HomeRazor_HasNoIngestHeading()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Pages", "Find", "Home.razor"));
        text.Should().NotContain("Ingest_Heading");
    }

    [Fact]
    public void GpuAverage_Empty_IsNull() => ResourceSampleMath.GpuAverage([]).Should().BeNull();

    [Fact]
    public void GpuAverage_TwoValues_IsMean() => ResourceSampleMath.GpuAverage([10f, 30f]).Should().Be(20);

    [Fact]
    public void GpuAverage_OverHundred_Clamps() => ResourceSampleMath.GpuAverage([150f]).Should().Be(100);

    [Fact]
    public void GpuAverage_Null_Throws() =>
        new Action(() => ResourceSampleMath.GpuAverage(null!)).Should().Throw<ArgumentNullException>();

    [Fact]
    public void Sample_NegativeRam_Throws() =>
        new Action(() => new ResourceSample(-1, 0, null)).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Sample_NullGpu_IsAllowed()
    {
        var sample = new ResourceSample(0, 0, null);
        sample.GpuPercent.Should().BeNull();
    }

    [Fact]
    public void AppShell_ShowsResourceStrip()
    {
        var shell = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Layout", "AppShell.razor"));
        var header = shell[shell.IndexOf("<header", StringComparison.Ordinal)..shell.IndexOf("</header>", StringComparison.Ordinal)];
        header.Should().NotContain("<ResourceStrip />");
        var nav = shell[shell.IndexOf("<nav", StringComparison.Ordinal)..shell.IndexOf("</nav>", StringComparison.Ordinal)];
        nav.Should().Contain("<ResourceStrip />");
        var strip = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Layout", "ResourceStrip.razor"));
        strip.Should().Contain("Resources_Ram");
        strip.Should().Contain("Resources_Cpu");
        strip.Should().Contain("Resources_Gpu");
    }

    [Fact]
    public void SkipFilter_Blank_IsTrue() => SkipListFilter.Matches(".cs", "  ").Should().BeTrue();

    [Fact]
    public void SkipFilter_Hit_IsTrue() => SkipListFilter.Matches("node_modules", "NODE").Should().BeTrue();

    [Fact]
    public void SkipFilter_Miss_IsFalse() => SkipListFilter.Matches(".cs", "py").Should().BeFalse();

    [Fact]
    public void SkipFilter_NullRow_Throws() =>
        new Action(() => SkipListFilter.Matches(null!, "x")).Should().Throw<ArgumentNullException>();

    [Fact]
    public void PreferencesRazor_AddBeforeBuiltInList()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Pages", "Library", "Preferences.razor"));
        text.IndexOf("extensionDraft", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("BuiltInSkips.CodeExtensions", StringComparison.Ordinal));
    }

    [Fact]
    public void PreferencesRazor_BuiltInHasNoRemove()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Pages", "Library", "Preferences.razor"));
        var blocks = text.Split("@foreach", StringSplitOptions.None);
        var builtIn = 0;
        var added = 0;
        foreach (var block in blocks.Skip(1))
        {
            var head = block.Length > 180 ? block[..180] : block;
            if (head.Contains("BuiltInSkips", StringComparison.Ordinal))
            {
                block.Should().NotContain("Skips_Remove");
                builtIn++;
            }

            if (head.Contains("skips.Extensions", StringComparison.Ordinal)
                || head.Contains("skips.FolderNames", StringComparison.Ordinal)
                || head.Contains("skips.FileNames", StringComparison.Ordinal))
            {
                block.Should().Contain("Skips_Remove");
                added++;
            }
        }

        builtIn.Should().BeGreaterThan(0);
        added.Should().Be(3);
    }
}
