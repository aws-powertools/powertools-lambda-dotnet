using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

public interface IBatchProcessor<in TEvent, out TRecord>
{
    BatchResponse BatchResponse { get; }
    Task<BatchResponse> ProcessAsync(TEvent @event, IRecordHandler<TRecord> recordHandler);
}
