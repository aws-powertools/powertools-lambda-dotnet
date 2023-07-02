using System.Threading;

namespace AWS.Lambda.Powertools.BatchProcessing;
public class ProcessingOptions
{
    public CancellationToken? CancellationToken { get; set; }
    public int? MaxDegreeOfParallelism { get; set; }
    public BatchProcessorErrorHandlingPolicy? ErrorHandlingPolicy { get; set; }
}
