# AWS.Lambda.PowerTools.Logging

This package contains classes that can be used as....

## Sample Function

Below is a sample class and Lambda function that illustrates how....

```csharp
public class Function
{
    private static readonly HttpClient client = new HttpClient();

    [Logging(LogEvent = true, SamplingRate = 0.7)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {                                
        try
        {
            var body = new Dictionary<string, string>
            {
                { "message", "hello world" },
                { "location", location }
            };
            
            // Append Log Key
            Logger.AppendKey("test", "willBeLogged");
            Logger.LogInformation($"log something out for inline subsegment 1");
            
            Task.WaitAll(task, anotherTask);

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(ex.Message),
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
```