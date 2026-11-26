using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AWS.Lambda.Powertools.ConcurrencyTests;

/// <summary>
///     Many tests in this assembly block thread-pool threads (<c>Barrier.SignalAndWait</c>, <c>Wait()</c>,
///     <c>Thread.Sleep</c>) while xUnit runs the test classes in parallel. On small CI runners the thread pool only
///     adds threads slowly, so short <c>Task.Run</c> work can wait for seconds and time out (#1377). Starting with
///     enough threads lets that work run right away.
/// </summary>
internal static class ThreadPoolSetup
{
    internal const int MinimumWorkerThreads = 64;

    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Test assembly: configures the thread pool once for all tests before any of them run.")]
    internal static void RaiseMinimumThreads()
    {
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
        if (workerThreads < MinimumWorkerThreads)
        {
            ThreadPool.SetMinThreads(MinimumWorkerThreads, completionPortThreads);
        }
    }
}
