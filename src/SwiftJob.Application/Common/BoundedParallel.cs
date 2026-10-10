using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace SwiftJob.Application.Common;

public static class BoundedParallel
{
    /// <summary>
    /// Runs <paramref name="work"/> for each item with at most <paramref name="maxConcurrency"/> in flight and
    /// yields results in completion order. Consumers process results one at a time, so non-thread-safe work
    /// (such as saving with a DbContext) can stay in the loop body while the slow calls run in parallel.
    /// Stopping the enumeration early cancels the remaining work.
    /// </summary>
    public static async IAsyncEnumerable<(TItem Item, TResult Result)> RunAsync<TItem, TResult>(
        IReadOnlyList<TItem> items,
        int maxConcurrency,
        Func<TItem, CancellationToken, Task<TResult>> work,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var results = Channel.CreateUnbounded<(TItem, TResult)>();
        using var gate = new SemaphoreSlim(Math.Max(1, maxConcurrency));

        async Task RunOne(TItem item)
        {
            await gate.WaitAsync(stop.Token);
            try
            {
                var result = await work(item, stop.Token);
                await results.Writer.WriteAsync((item, result), stop.Token);
            }
            finally
            {
                gate.Release();
            }
        }

        var tasks = items.Select(RunOne).ToList();
        var all = Task.WhenAll(tasks).ContinueWith(
            t => results.Writer.TryComplete(t.Exception?.GetBaseException() is { } e and not OperationCanceledException ? e : null),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        try
        {
            await foreach (var result in results.Reader.ReadAllAsync(cancellationToken))
            {
                yield return result;
            }
        }
        finally
        {
            await stop.CancelAsync();
            try
            {
                await all;
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // Remaining work was cancelled because the consumer stopped early.
            }
            catch (Exception) when (stop.IsCancellationRequested)
            {
                // Failures after cancellation are irrelevant to the consumer.
            }
        }
    }
}
