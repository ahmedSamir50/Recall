namespace Recall.Core.Checkpoints;

public sealed class CheckpointLease : IDisposable
{
    private readonly Action commitAction;
    private readonly Action rollbackAction;
    private bool committed;
    private bool disposed;

    internal CheckpointLease(Action commitAction, Action rollbackAction)
    {
        this.commitAction = commitAction;
        this.rollbackAction = rollbackAction;
    }

    public void Commit()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CheckpointLease));
        }

        committed = true;
        commitAction();
        disposed = true;
    }

    public void Rollback()
    {
        if (disposed)
        {
            return;
        }

        rollbackAction();
        disposed = true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (!committed)
        {
            rollbackAction();
        }

        disposed = true;
    }
}
