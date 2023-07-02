using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;
public interface IBatchProcessor<in TEvent, out TRecord>
{
    BatchResponse BatchResponse { get; }
    Task<BatchResponse> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler);
    Task<BatchResponse> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, CancellationToken cancellationToken);
    Task<BatchResponse> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler, ProcessingOptions processingOptions);
}
