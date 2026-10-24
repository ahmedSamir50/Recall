namespace Recall.Core.Ocr;

public static class OcrPassStub
{
    private static int openCount;

    public static int OpenCount => Volatile.Read(ref openCount);

    public static void Reset() => Volatile.Write(ref openCount, 0);

    public static void Run(Action processJobs)
    {
        Interlocked.Increment(ref openCount);
        processJobs();
    }
}
