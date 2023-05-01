namespace AWS.Lambda.Powertools.BatchProcessing;
public interface IRecordHandlerProvider<in TRecord>
{
    IRecordHandler<TRecord> Create();
}
