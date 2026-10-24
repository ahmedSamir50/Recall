using FluentAssertions;
using Recall.Adapters.Embedding.Llama;

namespace Recall.UnitTests;

public sealed class LlamaNativeGateTests
{
    [Fact(Timeout = 15000)]
    public void Enter_ConcurrentCalls_OneThreadInside()
    {
        // Arrange
        var token = TestContext.Current.CancellationToken;
        const int threadCount = 8;
        const int loops = 100;
        var inside = 0;
        var overlap = 0;
        var ready = new CountdownEvent(threadCount);
        var start = new ManualResetEventSlim(false);
        var workers = new Thread[threadCount];

        // Act
        for (var index = 0; index < threadCount; index++)
        {
            workers[index] = new Thread(() =>
            {
                ready.Signal();
                start.Wait(token);
                for (var loop = 0; loop < loops; loop++)
                {
                    LlamaNativeGate.Enter();
                    try
                    {
                        var now = Interlocked.Increment(ref inside);
                        if (now != 1)
                        {
                            Interlocked.Increment(ref overlap);
                        }

                        Thread.SpinWait(40);
                        Interlocked.Decrement(ref inside);
                    }
                    finally
                    {
                        LlamaNativeGate.Exit();
                    }
                }
            });
            workers[index].IsBackground = true;
            workers[index].Start();
        }

        ready.Wait(TimeSpan.FromSeconds(5), token).Should().BeTrue();
        start.Set();
        foreach (var worker in workers)
        {
            worker.Join(TimeSpan.FromSeconds(10)).Should().BeTrue();
        }

        // Assert
        overlap.Should().Be(0);
        inside.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public void Enter_NestedOnHolder_BlocksOtherThreadUntilOuterExit()
    {
        // Arrange
        var token = TestContext.Current.CancellationToken;
        var otherEntered = new ManualResetEventSlim(false);
        var other = new Thread(() =>
        {
            LlamaNativeGate.Enter();
            otherEntered.Set();
            LlamaNativeGate.Exit();
        });
        other.IsBackground = true;

        // Act
        LlamaNativeGate.Enter(TestContext.Current.CancellationToken);
        try
        {
            LlamaNativeGate.Enter(TestContext.Current.CancellationToken);
            try
            {
                other.Start();
                otherEntered.Wait(TimeSpan.FromMilliseconds(200), token).Should().BeFalse();
            }
            finally
            {
                LlamaNativeGate.Exit();
            }

            otherEntered.Wait(TimeSpan.FromMilliseconds(200), token).Should().BeFalse();
        }
        finally
        {
            LlamaNativeGate.Exit();
        }

        // Assert
        other.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
        otherEntered.IsSet.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task EnterAsync_WhileHeldOnSameThread_ReturnsWithoutWaiting()
    {
        // Arrange
        var token = TestContext.Current.CancellationToken;
        var otherEntered = new ManualResetEventSlim(false);
        var other = new Thread(() =>
        {
            LlamaNativeGate.Enter();
            otherEntered.Set();
            LlamaNativeGate.Exit();
        });
        other.IsBackground = true;

        // Act
        EnterGate();
        try
        {
            await LlamaNativeGate.EnterAsync(token);
            try
            {
                other.Start();
                otherEntered.Wait(TimeSpan.FromMilliseconds(200), token).Should().BeFalse();
            }
            finally
            {
                LlamaNativeGate.Exit();
            }

            otherEntered.Wait(TimeSpan.FromMilliseconds(200), token).Should().BeFalse();
        }
        finally
        {
            LlamaNativeGate.Exit();
        }

        // Assert
        other.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
        otherEntered.IsSet.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task EnterAsync_ConcurrentCalls_OneTaskInside()
    {
        // Arrange
        var token = TestContext.Current.CancellationToken;
        const int taskCount = 8;
        const int loops = 40;
        var inside = 0;
        var overlap = 0;
        var workers = new Task[taskCount];

        // Act
        for (var index = 0; index < taskCount; index++)
        {
            workers[index] = Task.Run(async () =>
            {
                for (var loop = 0; loop < loops; loop++)
                {
                    await LlamaNativeGate.EnterAsync(CancellationToken.None).ConfigureAwait(false);
                    try
                    {
                        var now = Interlocked.Increment(ref inside);
                        if (now != 1)
                        {
                            Interlocked.Increment(ref overlap);
                        }

                        Thread.SpinWait(40);
                        Interlocked.Decrement(ref inside);
                    }
                    finally
                    {
                        LlamaNativeGate.Exit();
                    }
                }
            }, token);
        }

        var finished = Task.WhenAll(workers);
        var completed = await Task.WhenAny(finished, Task.Delay(TimeSpan.FromSeconds(10), token));

        // Assert
        completed.Should().Be(finished);
        overlap.Should().Be(0);
        inside.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public async Task Enter_CancelledWhileAnotherThreadHolds_LeavesHolderInside()
    {
        // Arrange
        var token = TestContext.Current.CancellationToken;
        var holderInside = new ManualResetEventSlim(false);
        var releaseHolder = new ManualResetEventSlim(false);
        var holder = new Thread(() =>
        {
            LlamaNativeGate.Enter();
            holderInside.Set();
            releaseHolder.Wait();
            LlamaNativeGate.Exit();
        });
        holder.IsBackground = true;
        holder.Start();
        holderInside.Wait(TimeSpan.FromSeconds(5), token).Should().BeTrue();
        using var cts = new CancellationTokenSource();
        Exception? caught = null;
        var waiterStarted = new ManualResetEventSlim(false);
        var waiter = new Thread(() =>
        {
            waiterStarted.Set();
            try
            {
                LlamaNativeGate.Enter(cts.Token);
                LlamaNativeGate.Exit();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });
        waiter.IsBackground = true;
        var intruderInside = new ManualResetEventSlim(false);
        var intruder = new Thread(() =>
        {
            LlamaNativeGate.Enter();
            intruderInside.Set();
            LlamaNativeGate.Exit();
        });
        intruder.IsBackground = true;

        // Act
        waiter.Start();
        waiterStarted.Wait(TimeSpan.FromSeconds(5), token).Should().BeTrue();
        var stillHeld = new ManualResetEventSlim(false);
        stillHeld.Wait(TimeSpan.FromMilliseconds(100), token).Should().BeFalse();
        await cts.CancelAsync();
        waiter.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
        intruder.Start();
        var slipped = intruderInside.Wait(TimeSpan.FromMilliseconds(200), token);

        // Assert
        caught.Should().BeAssignableTo<OperationCanceledException>();
        slipped.Should().BeFalse();
        releaseHolder.Set();
        holder.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
        intruder.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
        intruderInside.IsSet.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task CountTokens_CancelledWhileGateHeld_DoesNotRunBesideHolder()
    {
        // Arrange
        var token = TestContext.Current.CancellationToken;
        var library = new BlockingCountLibrary();
        var counter = new LlamaTextTokenCounter(library);
        var holder = new Thread(() => counter.CountTokens("red planet"));
        holder.IsBackground = true;
        holder.Start();
        library.FirstEntered.Wait(TimeSpan.FromSeconds(5), token).Should().BeTrue();
        using var cts = new CancellationTokenSource();
        Exception? caught = null;
        var cancelledStarted = new ManualResetEventSlim(false);
        var cancelled = new Thread(() =>
        {
            cancelledStarted.Set();
            try
            {
                counter.CountTokens("red planet", cts.Token);
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });
        cancelled.IsBackground = true;
        var thirdStarted = new ManualResetEventSlim(false);
        var thirdFinished = new ManualResetEventSlim(false);
        var third = new Thread(() =>
        {
            thirdStarted.Set();
            counter.CountTokens("red planet");
            thirdFinished.Set();
        });
        third.IsBackground = true;
        var pause = new ManualResetEventSlim(false);

        // Act
        cancelled.Start();
        cancelledStarted.Wait(TimeSpan.FromSeconds(5), token).Should().BeTrue();
        pause.Wait(TimeSpan.FromMilliseconds(100), token).Should().BeFalse();
        await cts.CancelAsync();
        cancelled.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
        third.Start();
        thirdStarted.Wait(TimeSpan.FromSeconds(5), token).Should().BeTrue();
        pause.Wait(TimeSpan.FromMilliseconds(200), token).Should().BeFalse();
        var callsWhileHeld = Volatile.Read(ref library.CountCalls);

        // Assert
        caught.Should().BeAssignableTo<OperationCanceledException>();
        callsWhileHeld.Should().Be(1);
        thirdFinished.Wait(TimeSpan.Zero, token).Should().BeFalse();
        library.FirstRelease.Set();
        holder.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
        third.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
        library.CountCalls.Should().Be(2);
    }

    private static void EnterGate() => LlamaNativeGate.Enter();

    private sealed class BlockingCountLibrary : IRecallEmbedLibrary
    {
        public int CountCalls;

        public ManualResetEventSlim FirstEntered { get; } = new(false);

        public ManualResetEventSlim FirstRelease { get; } = new(false);

        public int CountTokens(string text)
        {
            var call = Interlocked.Increment(ref CountCalls);
            if (call == 1)
            {
                FirstEntered.Set();
                FirstRelease.Wait();
            }

            return 1;
        }

        public int Open(string modelPath) => 0;

        public void Close()
        {
        }

        public int[] TokenEnds(string text) => [];

        public int Embed(string text, float[] destination768) => 0;

        public int OpenVision(string mmprojPath) => 0;

        public int EmbedVision(byte[] jpeg, float[] destination768) => 0;

        public void CloseVision()
        {
        }

        public int OpenAudio(string mmprojPath) => 0;

        public int EmbedAudio(float[] samples, float[] destination768) => 0;

        public void CloseAudio()
        {
        }
    }
}
