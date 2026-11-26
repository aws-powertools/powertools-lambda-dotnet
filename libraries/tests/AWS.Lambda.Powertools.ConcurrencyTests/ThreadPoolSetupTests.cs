using Xunit;

namespace AWS.Lambda.Powertools.ConcurrencyTests;

public class ThreadPoolSetupTests
{
    [Fact]
    public void ThreadPool_HasEnoughMinimumWorkerThreadsForTheBlockingTests()
    {
        ThreadPool.GetMinThreads(out var workerThreads, out _);

        Assert.True(workerThreads >= ThreadPoolSetup.MinimumWorkerThreads,
            $"Expected at least {ThreadPoolSetup.MinimumWorkerThreads} minimum worker threads, got {workerThreads}.");
    }
}
