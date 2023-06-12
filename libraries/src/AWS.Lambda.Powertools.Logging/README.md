# AWS.Lambda.Powertools.Logging

The logging utility provides a [AWS Lambda](https://aws.amazon.com/lambda/) optimized logger with output structured as JSON.

## Key features

* Capture key fields from Lambda context, cold start and structures logging output as JSON
* Log Lambda event when instructed (disabled by default)
* Log sampling enables DEBUG log level for a percentage of requests (disabled by default)
* Append additional keys to structured log at any point in time

## Read the docs

For a full list of features go to [docs.powertools.aws.dev/lambda-dotnet/core/logging/](docs.powertools.aws.dev/lambda-dotnet/core/logging/)

GitHub: https://github.com/aws-powertools/lambda-dotnet/

## Sample Function

```csharp
public class Function
{
    [Logging(LogEvent = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigwProxyEvent,
        ILambdaContext context)
    {
        var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;
        
        var lookupInfo = new Dictionary<string, object>()
        {
            {"LookupInfo", new Dictionary<string, object>{{ "LookupId", requestContextRequestId }}}
        };

        // Appended keys are added to all subsequent log entries in the current execution.
        // Call this method as early as possible in the Lambda handler.
        // Typically this is value would be passed into the function via the event.
        // Set the ClearState = true to force the removal of keys across invocations,
        Logger.AppendKeys(lookupInfo);

        Logger.LogInformation("Getting ip address from external service");

        var location = await GetCallingIp();

        var lookupRecord = new LookupRecord(lookupId: requestContextRequestId,
            greeting: "Hello AWS Lambda Powertools for .NET", ipAddress: location);

        try
        {
            await SaveRecordInDynamo(lookupRecord);
            
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(lookupRecord),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);

            return new APIGatewayProxyResponse
            {
                Body = e.Message,
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
```

## Sample output

```json
{
    "cold_start": false,
    "xray_trace_id": "1-623d34cb-00c3698b02f11dc713442693",
    "lookup": {
        "lookup_id": "7d2ce9bb-c7d1-4304-9912-6078d276604f"
    },
    "function_name": "PowertoolsLoggingSample-HelloWorldFunction-hm1r10VT3lCy",
    "function_version": "$LATEST",
    "function_memory_size": 256,
    "function_arn": "arn:aws:lambda:ap-southeast-2:111111111111:function:PowertoolsLoggingSample-HelloWorldFunction-hm1r10VT3lCy",
    "function_request_id": "f5c3bae7-0e18-495a-9959-ef101c7afbc0",
    "timestamp": "2022-03-25T03:19:39.9322301Z",
    "level": "Information",
    "service": "powertools-dotnet-logging-sample",
    "name": "AWS.Lambda.Powertools.Logging.Logger",
    "message": "Getting ip address from external service"
}
```
