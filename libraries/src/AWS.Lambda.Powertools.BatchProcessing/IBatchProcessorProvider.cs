namespace AWS.Lambda.Powertools.BatchProcessing;

public interface IBatchProcessorProvider<in TEvent, out TRecord>
{
    IBatchProcessor<TEvent, TRecord> Create();
}
