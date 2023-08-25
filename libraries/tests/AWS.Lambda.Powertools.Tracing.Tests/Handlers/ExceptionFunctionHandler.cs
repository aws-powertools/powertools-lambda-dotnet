using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.Tracing.Tests.Handlers;

public class ExceptionFunctionHandler
{
    [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
    public async Task<string> Handle(string input)
    {
        ThisThrows();

        await Task.Delay(1);

        return input.ToUpper(CultureInfo.InvariantCulture);
    }

    private void ThisThrows()
    {
        throw new NullReferenceException();
    }
}