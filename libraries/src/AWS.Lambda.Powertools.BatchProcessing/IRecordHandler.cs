using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing;

public interface IRecordHandler<in TRecord>
{
    Task HandleAsync(TRecord record);
}
