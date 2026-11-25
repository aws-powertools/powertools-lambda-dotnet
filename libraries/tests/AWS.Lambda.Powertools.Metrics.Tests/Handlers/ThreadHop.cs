using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

/// <summary>
/// Deterministic thread switches for async tests. Awaiting these never completes synchronously,
/// so the code after the await always runs on a different thread than the code before it,
/// while the ExecutionContext still flows (like a normal await resuming on a thread-pool thread).
/// </summary>
internal static class ThreadHop
{
    /// <summary>
    /// Resumes the awaiting method on a brand-new thread.
    /// </summary>
    public static NewThreadAwaitable ToNewThread() => default;

    internal readonly struct NewThreadAwaitable : ICriticalNotifyCompletion
    {
        public NewThreadAwaitable GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => Start(continuation);
        public void UnsafeOnCompleted(Action continuation) => Start(continuation);

        private static void Start(Action continuation) =>
            new Thread(() => continuation()) { IsBackground = true }.Start();
    }
}

/// <summary>
/// A single dedicated thread that runs posted continuations. Lets a test make two invocations
/// run on the same thread, which is how per-thread state leaks between invocations.
/// </summary>
internal sealed class DedicatedThread : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    public DedicatedThread()
    {
        _thread = new Thread(() =>
        {
            foreach (var action in _queue.GetConsumingEnumerable())
                action();
        }) { IsBackground = true };
        _thread.Start();
    }

    public int ManagedThreadId => _thread.ManagedThreadId;

    /// <summary>
    /// Resumes the awaiting method on this dedicated thread.
    /// </summary>
    public SwitchAwaitable SwitchTo() => new(this);

    public void Dispose() => _queue.CompleteAdding();

    internal readonly struct SwitchAwaitable : ICriticalNotifyCompletion
    {
        private readonly DedicatedThread _owner;
        public SwitchAwaitable(DedicatedThread owner) => _owner = owner;
        public SwitchAwaitable GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => _owner._queue.Add(continuation);
        public void UnsafeOnCompleted(Action continuation) => _owner._queue.Add(continuation);
    }
}
