using AWS.Lambda.Powertools.BatchProcessing;

namespace AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing.Helpers;

/// <summary>
/// Represents the result of a concurrent invocation test.
/// </summary>
/// <typeparam name="TRecord">The type of batch record.</typeparam>
public class ConcurrentInvocationResult<TRecord>
{
    /// <summary>
    /// Gets or sets the unique identifier for this invocation.
    /// </summary>
    public string InvocationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected number of records in the batch.
    /// </summary>
    public int ExpectedRecordCount { get; set; }

    /// <summary>
    /// Gets or sets the expected number of failures.
    /// </summary>
    public int ExpectedFailureCount { get; set; }

    /// <summary>
    /// Gets or sets the expected record identifiers.
    /// </summary>
    public HashSet<string> ExpectedRecordIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the actual processing result.
    /// </summary>
    public ProcessingResult<TRecord>? ActualResult { get; set; }

    /// <summary>
    /// Gets or sets any exception that occurred during processing.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the duration of the processing.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets whether the invocation completed successfully (no exception).
    /// </summary>
    public bool IsSuccess => Exception == null;

    /// <summary>
    /// Gets whether the actual record count matches the expected count.
    /// </summary>
    public bool HasCorrectRecordCount => ActualResult?.BatchRecords.Count == ExpectedRecordCount;

    /// <summary>
    /// Gets whether the actual failure count matches the expected count.
    /// </summary>
    public bool HasCorrectFailureCount => ActualResult?.FailureRecords.Count == ExpectedFailureCount;
}

/// <summary>
/// Context for tracking batch processing test state.
/// </summary>
/// <typeparam name="TRecord">The type of batch record.</typeparam>
public class BatchProcessingTestContext<TRecord>
{
    /// <summary>
    /// Gets or sets the unique identifier for this invocation.
    /// </summary>
    public string InvocationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of input records.
    /// </summary>
    public int InputRecordCount { get; set; }

    /// <summary>
    /// Gets or sets the input record identifiers.
    /// </summary>
    public HashSet<string> InputRecordIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the processing result.
    /// </summary>
    public ProcessingResult<TRecord>? Result { get; set; }

    /// <summary>
    /// Gets or sets the record IDs from the result.
    /// </summary>
    public HashSet<string> ResultRecordIds { get; set; } = new();

    /// <summary>
    /// Gets whether the result contains only records from this invocation's input.
    /// </summary>
    public bool ResultContainsOnlyOwnRecords => 
        ResultRecordIds.Count > 0 && ResultRecordIds.IsSubsetOf(InputRecordIds);

    /// <summary>
    /// Gets whether the result contains all records from this invocation's input.
    /// </summary>
    public bool ResultContainsAllOwnRecords => 
        InputRecordIds.IsSubsetOf(ResultRecordIds);
}

/// <summary>
/// Helper class for running concurrent invocation tests.
/// </summary>
public static class ConcurrentInvocationHelper
{
    /// <summary>
    /// Runs multiple concurrent invocations with barrier synchronization.
    /// </summary>
    /// <typeparam name="TResult">The type of result from each invocation.</typeparam>
    /// <param name="concurrencyLevel">Number of concurrent invocations.</param>
    /// <param name="invocationAction">The action to run for each invocation.</param>
    /// <returns>Array of results from all invocations.</returns>
    public static TResult[] RunConcurrentInvocations<TResult>(
        int concurrencyLevel,
        Func<int, Barrier, TResult> invocationAction)
    {
        var results = new TResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(() =>
            {
                results[invocationIndex] = invocationAction(invocationIndex, barrier);
            });
        }

        Task.WaitAll(tasks);
        return results;
    }

    /// <summary>
    /// Runs multiple concurrent async invocations with barrier synchronization.
    /// </summary>
    /// <typeparam name="TResult">The type of result from each invocation.</typeparam>
    /// <param name="concurrencyLevel">Number of concurrent invocations.</param>
    /// <param name="invocationAction">The async action to run for each invocation.</param>
    /// <returns>Array of results from all invocations.</returns>
    public static async Task<TResult[]> RunConcurrentInvocationsAsync<TResult>(
        int concurrencyLevel,
        Func<int, Barrier, Task<TResult>> invocationAction)
    {
        var results = new TResult[concurrencyLevel];
        var barrier = new Barrier(concurrencyLevel);
        var tasks = new Task[concurrencyLevel];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            int invocationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                results[invocationIndex] = await invocationAction(invocationIndex, barrier);
            });
        }

        await Task.WhenAll(tasks);
        return results;
    }
}
