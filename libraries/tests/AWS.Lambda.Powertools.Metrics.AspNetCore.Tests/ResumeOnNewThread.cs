using System.Runtime.CompilerServices;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Tests;

/// <summary>
/// Never completes synchronously and resumes on a brand-new thread, so the code after the
/// await always runs on a different thread than the code before it.
/// </summary>
internal readonly struct ResumeOnNewThread : ICriticalNotifyCompletion
{
    public ResumeOnNewThread GetAwaiter() => this;
    public bool IsCompleted => false;
    public void GetResult() { }
    public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
    public void UnsafeOnCompleted(Action continuation) =>
        new Thread(() => continuation()) { IsBackground = true }.Start();
}
