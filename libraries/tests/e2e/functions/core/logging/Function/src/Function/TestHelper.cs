using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Logging;

namespace Helpers;

public static class TestHelper
{
    public static void TestMethod(APIGatewayProxyRequest apigwProxyEvent)
    {
        Logger.LogInformation("Processing request started");
        
        var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;
        var lookupInfo = new Dictionary<string, object>()
        {
            {"LookupInfo", new Dictionary<string, object>{{ "LookupId", requestContextRequestId }}}
        };  
        
        var customKeys = new Dictionary<string, string>
        {
            {"test1", "value1"}, 
            {"test2", "value2"}
        };
        
        Logger.AppendKeys(lookupInfo);
        Logger.AppendKeys(customKeys);
        
        Logger.LogWarning("Warn with additional keys");
        
        Logger.RemoveKeys("test1", "test2");
        
        var error = new InvalidOperationException("Parent exception message",
            new ArgumentNullException(nameof(apigwProxyEvent),
                new Exception("Very important nested inner exception message")));
        Logger.LogError(error, "Oops something went wrong");
    }
}