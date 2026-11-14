namespace Bookstore.AppHost.Tests;

public static class Polling
{
    // Asks again until the condition holds; fails with the last value instead of hanging
    public static async Task<T> UntilAsync<T>(Func<CancellationToken, Task<T>> probe, Func<T, bool> condition, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        T last = await probe(cts.Token);
        while (!condition(last))
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
                last = await probe(cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                Assert.Fail($"Condition not met within {timeout.TotalSeconds} s. Last value: {last}");
            }
        }

        return last;
    }
}
