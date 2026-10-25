using FluentAssertions;
using Recall.Core.Faults;
using Recall.Core.Ingest;
using Recall.Core.Ocr;
using Recall.Core.Ports;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class CloseOpenOcrTests
{
    [Fact]
    public void DbBox_Null_Throws()
    {
        var act = () => DbBoxExtractor.Extract(null!, 1, 1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DbBox_BadLength_Throws()
    {
        var act = () => DbBoxExtractor.Extract([0.5f], 2, 2);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DbBox_BelowThresh_IsEmpty()
    {
        DbBoxExtractor.Extract([0.1f, 0.1f, 0.1f, 0.1f], 2, 2).Should().BeEmpty();
    }

    [Fact]
    public void DbBox_TwoComponents_SortsByYThenX()
    {
        var map = new float[100];
        for (var y = 2; y < 6; y++)
        {
            for (var x = 2; x < 8; x++)
            {
                map[y * 10 + x] = 0.9f;
            }
        }

        for (var y = 12; y < 16; y++)
        {
            for (var x = 2; x < 8; x++)
            {
                map[y * 10 + x] = 0.9f;
            }
        }

        var boxes = DbBoxExtractor.Extract(map, 10, 10);
        boxes.Should().HaveCount(2);
        boxes[0].Y.Should().BeLessThan(boxes[1].Y);
    }

    [Fact]
    public void Ctc_AllBlank_IsEmpty()
    {
        var logits = new float[10];
        CtcDecoder.Decode(logits, 5, 2, ["a"]).Should().Be(("", 0));
    }

    [Fact]
    public void Ctc_Duplicate_Collapses()
    {
        var logits = new float[6];
        logits[2] = 5f;
        logits[5] = 5f;
        CtcDecoder.Decode(logits, 2, 3, ["a", "b"]).Text.Should().Be("b");
    }

    [Fact]
    public void Ctc_Null_Throws()
    {
        var act = () => CtcDecoder.Decode(null!, 1, 2, ["a"]);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Route_FirstThree_SetPageReader()
    {
        var lines =
        [
            new OcrLinePair("en", 0.2, "ar", 0.9, true),
            new OcrLinePair("en", 0.2, "ar", 0.8, true),
            new OcrLinePair("en", 0.2, "ar", 0.7, true),
            new OcrLinePair("en", 0.9, "ar", 0.1, false),
        ];
        OcrPageRouter.Route(lines).Should().Contain("ar");
    }

    [Fact]
    public void Route_LowScore_TriesOther()
    {
        var lines = [new OcrLinePair("strong", 0.95, "weak", 0.1, false)];
        OcrPageRouter.Route(lines).Should().Be("strong");
    }

    [Fact]
    public void Route_Tie_KeepsEnglish()
    {
        var lines =
        [
            new OcrLinePair("en", 0.8, "ar", 0.8, false),
            new OcrLinePair("en", 0.8, "ar", 0.8, false),
            new OcrLinePair("en", 0.8, "ar", 0.8, false),
        ];
        OcrPageRouter.Route(lines).Should().Contain("en");
    }

    [Fact]
    public void Route_Null_Throws()
    {
        var act = () => OcrPageRouter.Route(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChineseReader_NotInCatalog()
    {
        OcrModelFiles.LocalNames.Should().HaveCount(3);
        OcrModelFiles.LocalNames.Should().NotContain(name => name.Contains("zh", StringComparison.OrdinalIgnoreCase) || name.Contains("chinese", StringComparison.OrdinalIgnoreCase));
        OcrReaderGate.CanLoad("deadbeef").Should().BeFalse();
    }

    [Fact]
    public async Task OcrPass_LoadsOnce_ForManyPages()
    {
        FakeOcrEngine.Reset();
        var scheduler = new IngestScheduler();
        var runner = new IngestSchedulerRunner(scheduler);
        scheduler.Enqueue(new IngestJob(IngestModel.Ocr, "a.png"));
        scheduler.Enqueue(new IngestJob(IngestModel.Ocr, "b.png"));
        var hooks = new IngestPassHooks { RunOcr = _ => new FakeOcrEngine() };
        await runner.RunReadyPassesAsync(DateTimeOffset.UtcNow.AddSeconds(31), hooks, TestContext.Current.CancellationToken);
        FakeOcrEngine.CreateCount.Should().Be(1);
        FakeOcrEngine.OpenCount.Should().Be(0);
    }

    [Fact]
    public async Task Ocr_TextLayer_DoesNotOpen()
    {
        var engine = new FakeOcrEngine();
        var scheduler = new IngestScheduler();
        var runner = new IngestSchedulerRunner(scheduler);
        scheduler.Enqueue(new IngestJob(IngestModel.Ocr, @"C:\grant\text.pdf"));
        var hooks = new IngestPassHooks
        {
            RunOcr = paths => paths.All(p => p.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) ? new NoOpOcrEngine() : engine,
        };
        await runner.RunReadyPassesAsync(DateTimeOffset.UtcNow.AddSeconds(31), hooks, TestContext.Current.CancellationToken);
        engine.RunPassCalls.Should().Be(0);
    }

    [Fact]
    public void Ocr_UnloadedAfterJob()
    {
        using var engine = new FakeOcrEngine();
        engine.DetectorLoaded.Should().BeTrue();
        engine.Dispose();
        engine.DetectorLoaded.Should().BeFalse();
    }

    private sealed class FakeOcrEngine : IOcrEngine
    {
        private static int openCount;
        private static int createCount;

        public static int OpenCount => Volatile.Read(ref openCount);

        public static int CreateCount => Volatile.Read(ref createCount);

        public static void Reset()
        {
            Volatile.Write(ref openCount, 0);
            Volatile.Write(ref createCount, 0);
        }

        public int RunPassCalls { get; private set; }

        public int LastPathCount { get; private set; }

        public bool DetectorLoaded { get; private set; } = true;

        public FakeOcrEngine()
        {
            Interlocked.Increment(ref createCount);
            Interlocked.Increment(ref openCount);
        }

        public void RunPass(IReadOnlyList<string> paths)
        {
            RunPassCalls++;
            LastPathCount = paths.Count;
        }

        public void Dispose()
        {
            DetectorLoaded = false;
            Interlocked.Decrement(ref openCount);
        }
    }

    private sealed class NoOpOcrEngine : IOcrEngine
    {
        public void RunPass(IReadOnlyList<string> paths)
        {
        }

        public void Dispose()
        {
        }
    }
}
