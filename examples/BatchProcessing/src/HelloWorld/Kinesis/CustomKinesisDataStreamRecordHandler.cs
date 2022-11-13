using System.Threading.Tasks;
using Amazon.Lambda.KinesisEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.Logging;

namespace HelloWorld.Kinesis;
internal class CustomKinesisDataStreamRecordHandler : IRecordHandler<KinesisEvent.KinesisEventRecord>
{
    public async Task HandleAsync(KinesisEvent.KinesisEventRecord record)
    {
        Logger.LogInformation($"Handling Kinesis record with sequence number: '{record.Kinesis.SequenceNumber}'.");
        await Task.CompletedTask;
    }
}
