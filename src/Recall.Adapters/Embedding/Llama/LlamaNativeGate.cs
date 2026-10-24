namespace Recall.Adapters.Embedding.Llama;

internal static class LlamaNativeGate
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    [ThreadStatic]
    private static int depth;

    public static void Enter(CancellationToken cancellationToken = default)
    {
        if (depth > 0)
        {
            depth++;
            return;
        }

        Gate.Wait(cancellationToken);
        depth = 1;
    }

    public static async ValueTask EnterAsync(CancellationToken cancellationToken)
    {
        if (depth > 0)
        {
            depth++;
            return;
        }

        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        depth = 1;
    }

    public static void Exit()
    {
        if (depth > 1)
        {
            depth--;
            return;
        }

        depth = 0;
        Gate.Release();
    }
}
