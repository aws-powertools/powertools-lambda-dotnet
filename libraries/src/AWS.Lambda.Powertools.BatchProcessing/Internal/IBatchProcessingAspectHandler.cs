using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

internal interface IBatchProcessingAspectHandler
{
    Task HandleAsync(AspectEventArgs eventArgs);
}