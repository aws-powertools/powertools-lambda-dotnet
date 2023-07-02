using System;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;
public class RecordHandler<TRecord> : IRecordHandler<TRecord>
{
    private readonly Func<TRecord, Task> _handlerFunc;

    private RecordHandler(Func<TRecord, Task> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    public async Task HandleAsync(TRecord record, CancellationToken cancellationToken)
    {
        await _handlerFunc.Invoke(record);
    }

    public static IRecordHandler<TRecord> From(Action<TRecord> handlerAction)
    {
        return new RecordHandler<TRecord>(async x =>
        {
            handlerAction(x);
            await Task.CompletedTask;
        });
    }

    public static IRecordHandler<TRecord> From(Func<TRecord, Task> handlerFunc)
    {
        return new RecordHandler<TRecord>(handlerFunc);
    }
}
