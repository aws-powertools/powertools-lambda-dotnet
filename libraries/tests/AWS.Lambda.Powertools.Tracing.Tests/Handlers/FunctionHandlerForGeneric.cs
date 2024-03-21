using System.Globalization;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.Tracing.Tests.Handlers;

public class FunctionHandlerForGeneric
{   
    [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
    public async Task<string> Handle(string input)
    {
        GenericMethod<int>(1);
        GenericMethod<double>(2);
        
        GenericMethod<int>(1);
        
        GenericMethod<int>();
        GenericMethod<double>();
        
        GenericMethod2<int>(1);
        GenericMethod2<double>(2);
        
        GenericMethod<int>();
        GenericMethod<double>();

        await Task.Delay(1);

        return input.ToUpper(CultureInfo.InvariantCulture);
    }

    [Tracing]
    private T GenericMethod<T>(T x)
    {
        return default;
    }
    
    [Tracing]
    private T GenericMethod<T>()
    {
        return default;
    }
    
    [Tracing]
    private void GenericMethod2<T>(T x)
    {
    }
}