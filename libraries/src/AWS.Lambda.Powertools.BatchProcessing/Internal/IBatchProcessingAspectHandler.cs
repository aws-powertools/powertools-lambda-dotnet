using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

internal interface IBatchProcessingAspectHandler
{
    Task HandleAsync(object[] args);
}