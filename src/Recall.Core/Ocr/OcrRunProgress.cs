using System.Diagnostics;

namespace Recall.Core.Ocr;

public static class OcrRunProgress
{
    private static readonly object Gate = new();
    private static Stopwatch? passWatch;
    private static int lastHeartbeatSecond = -1;

    public static bool Enabled =>
        string.Equals(Environment.GetEnvironmentVariable("RECALL_OCR_PROGRESS"), "1", StringComparison.Ordinal);

    public static void BeginPass(int pathCount)
    {
        if (!Enabled)
        {
            return;
        }

        lock (Gate)
        {
            passWatch = Stopwatch.StartNew();
            lastHeartbeatSecond = -1;
            Write($"pass start paths={pathCount}");
        }
    }

    public static void BeginPath(int index, int total, string path)
    {
        if (!Enabled)
        {
            return;
        }

        lock (Gate)
        {
            var name = string.IsNullOrEmpty(path) ? "(empty)" : Path.GetFileName(path);
            Write($"path {index}/{total} start {name}");
        }
    }

    public static void EndPath(int index, int total, string path, int characterCount)
    {
        if (!Enabled)
        {
            return;
        }

        lock (Gate)
        {
            var elapsed = passWatch?.Elapsed.TotalSeconds ?? 0;
            var name = string.IsNullOrEmpty(path) ? "(empty)" : Path.GetFileName(path);
            Write($"path {index}/{total} done {name} chars={characterCount} elapsed={elapsed:F1}s");
            MaybeHeartbeat();
        }
    }

    public static void EndPass(int recognizedCount)
    {
        if (!Enabled)
        {
            return;
        }

        lock (Gate)
        {
            var elapsed = passWatch?.Elapsed.TotalSeconds ?? 0;
            Write($"pass end recognized={recognizedCount} elapsed={elapsed:F1}s");
            passWatch = null;
        }
    }

    public static void Message(string text)
    {
        if (!Enabled)
        {
            return;
        }

        lock (Gate)
        {
            Write(text);
        }
    }

    private static void MaybeHeartbeat()
    {
        var elapsed = passWatch?.Elapsed.TotalSeconds ?? 0;
        var second = (int)elapsed;
        if (second <= 0 || second == lastHeartbeatSecond || second % 15 != 0)
        {
            return;
        }

        lastHeartbeatSecond = second;
        Write($"heartbeat elapsed={elapsed:F1}s (still running)");
    }

    private static void Write(string line)
    {
        var stamp = DateTimeOffset.Now.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        Console.WriteLine($"[recall-ocr {stamp}] {line}");
        Console.Out.Flush();
    }
}
