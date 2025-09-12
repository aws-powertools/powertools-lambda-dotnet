

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Helper class to create inline record handlers.
/// </summary>
/// <typeparam name="TRecord">Type of batch record.</typeparam>
public class RecordHandler<TRecord> : IRecordHandler<TRecord>
{
    private readonly Func<TRecord, Task<RecordHandlerResult>> _handlerFunc;

    private RecordHandler(Func<TRecord, Task<RecordHandlerResult>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    /// <inheritdoc />
    public async Task<RecordHandlerResult> HandleAsync(TRecord record, CancellationToken cancellationToken)
    {
        return await _handlerFunc.Invoke(record);
    }

    /// <summary>
    /// Creates a record handler that uses the provided delegate for record processing.
    /// </summary>
    /// <param name="handlerAction">The delegate to use for record processing.</param>
    /// <returns>The created record handler.</returns>
    public static IRecordHandler<TRecord> From(Action<TRecord> handlerAction)
    {
        return new RecordHandler<TRecord>(async x =>
        {
            handlerAction(x);
            return await Task.FromResult(RecordHandlerResult.None);
        });
    }

    /// <summary>
    /// Creates a record handler that uses the provided async delegate for record processing.
    /// </summary>
    /// <param name="handlerFunc">The async delegate to use for record processing.</param>
    /// <returns>The created record handler.</returns>
    public static IRecordHandler<TRecord> From(Func<TRecord, Task<RecordHandlerResult>> handlerFunc)
    {
        return new RecordHandler<TRecord>(handlerFunc);
    }
}
