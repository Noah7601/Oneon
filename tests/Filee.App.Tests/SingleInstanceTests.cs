// A later launch hands its arguments to the running Filee, also while that one is still starting.

using Filee.App.Services;

namespace Filee.App.Tests;

public class SingleInstanceTests
{
    [Fact]
    public async Task Arguments_reach_a_first_instance_that_starts_listening_late()
    {
        // The installer's --quit (or a second launch) came 8 s after Filee started, on a computer where Filee took 12 s
        // to start listening: one 3-second try gave up and the request was lost.
        var name = "Filee-test-" + Guid.NewGuid().ToString("N")[..8];
        using var first = SingleInstance.Acquire(name);
        Assert.True(first.IsFirst);
        var received = new TaskCompletionSource<string[]>();
        first.ArgumentsReceived += args => received.TrySetResult(args);

        var forward = Task.Run(() =>
        {
            using var second = SingleInstance.Acquire(name);
            Assert.False(second.IsFirst);
            return second.ForwardToFirst(["--quit"], TimeSpan.FromSeconds(15));
        }, TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken); // longer than the old single try
        first.StartListening();

        Assert.True(await forward.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));
        Assert.Equal(["--quit"], await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_burst_of_later_launches_loses_nothing()
    {
        var name = "Filee-test-" + Guid.NewGuid().ToString("N")[..8];
        using var first = SingleInstance.Acquire(name);
        var received = new System.Collections.Concurrent.ConcurrentBag<string>();
        first.ArgumentsReceived += args => received.Add(args[^1]);
        first.StartListening();

        var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(i => Task.Factory.StartNew(() =>
        {
            using var later = SingleInstance.Acquire(name);
            return later.ForwardToFirst(["--convert", $"{i}.png"], TimeSpan.FromSeconds(15));
        }, TestContext.Current.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)));

        Assert.All(results, Assert.True);
        var waited = System.Diagnostics.Stopwatch.StartNew();
        while (received.Count < 50 && waited.Elapsed < TimeSpan.FromSeconds(10))
            await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.Equal(50, received.Distinct().Count());
    }

    [Fact]
    public async Task A_launch_that_never_sends_does_not_block_the_next()
    {
        var name = "Filee-test-" + Guid.NewGuid().ToString("N")[..8];
        using var first = SingleInstance.Acquire(name);
        var arrived = new TaskCompletionSource<string>();
        first.ArgumentsReceived += args => arrived.TrySetResult(args[^1]);
        first.ReadTimeout = TimeSpan.FromMilliseconds(200);
        first.StartListening();

        using var stalled = new System.IO.Pipes.NamedPipeClientStream(".", name, System.IO.Pipes.PipeDirection.Out,
            System.IO.Pipes.PipeOptions.CurrentUserOnly);
        await stalled.ConnectAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);

        _ = Task.Run(() =>
        {
            using var later = SingleInstance.Acquire(name);
            return later.ForwardToFirst(["--convert", "later.png"], TimeSpan.FromSeconds(15));
        }, TestContext.Current.CancellationToken);
        Assert.Equal("later.png", await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_listener_error_does_not_end_listening()
    {
        var name = "Filee-test-" + Guid.NewGuid().ToString("N")[..8];
        using var first = SingleInstance.Acquire(name);
        var arrived = new TaskCompletionSource<string>();
        var original = first.WaitForLaunch;
        var failed = 0;
        first.WaitForLaunch = (server, token) => Interlocked.Exchange(ref failed, 1) == 0
            ? Task.FromException(new IOException("test failure"))
            : original(server, token);
        first.ArgumentsReceived += args => arrived.TrySetResult(args[^1]);
        first.StartListening();

        await Task.Delay(600, TestContext.Current.CancellationToken);
        using var later = SingleInstance.Acquire(name);
        Assert.True(later.ForwardToFirst(["--convert", "later.png"], TimeSpan.FromSeconds(15)));
        Assert.Equal("later.png", await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Forwarding_gives_up_when_nobody_listens()
    {
        var name = "Filee-test-" + Guid.NewGuid().ToString("N")[..8];
        using var first = SingleInstance.Acquire(name);

        var delivered = await Task.Run(() =>
        {
            using var instance = SingleInstance.Acquire(name);
            return instance.ForwardToFirst(["--quit"], TimeSpan.FromSeconds(1));
        }, TestContext.Current.CancellationToken);

        Assert.False(delivered);
    }
}
