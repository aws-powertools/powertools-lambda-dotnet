using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Logging.Tests.Handlers;

public class ExceptionFunctionHandler
{
    [Logging(LogEvent = true)]
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
    
    [Logging(CorrelationIdPath = "/1//", LogEvent = true, Service = null, SamplingRate = 10000d)]
    public string HandlerLoggerForExceptions(string input, ILambdaContext context)
    { 
        // Edge cases and bad code to force exceptions 
        
        Logger.LogInformation("Hello {input}", input);
        Logger.LogError("Hello {input}", input);
        Logger.LogCritical("Hello {input}", input);
        Logger.LogDebug("Hello {input}", input);
        Logger.LogTrace("Hello {input}", input);
        
        Logger.LogInformation("Testing with parameter Log Information Method {company}", new[] { "AWS" });
        
        var customKeys = new Dictionary<string, string>
        {
            {"test1", "value1"}, 
            {"test2", "value2"}
        };
        Logger.LogInformation(customKeys, "Retrieved data for city {cityName} with count {company}", "AWS");

        Logger.AppendKey("aws",1);
        Logger.AppendKey("aws",3);
        
        Logger.RemoveKeys("test");
        
        Logger.AppendKeys(new[]{ new KeyValuePair<string, object>("aws",1), new KeyValuePair<string, object>("aws",2)});
        
        return "OK";
    }
}