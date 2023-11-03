namespace HelloWorld;

using System.Text.Json;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    private static HttpClient? _httpClient;
    private static IAmazonDynamoDB? _dynamoDbClient;

    /// <summary>
    /// Function constructor
    /// </summary>
    public Function()
    {
        Logger.SetSerializer(new SourceGeneratedSerializer<CustomSerializationContext>());

        AWSSDKHandler.RegisterXRayForAllServices();
        _httpClient = new HttpClient();
        _dynamoDbClient = new AmazonDynamoDBClient();
    }

    /// <summary>
    /// Test constructor
    /// </summary>
    public Function(IAmazonDynamoDB dynamoDbClient, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _dynamoDbClient = dynamoDbClient;
    }

    [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
    [Metrics]
    [Logging(LogEvent = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest apigwProxyEvent,
        ILambdaContext context)
    {
        var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;

        var lookupInfo = new Dictionary<string, object>
        {
            { "LookupInfo", new Dictionary<string, object> { { "LookupId", requestContextRequestId } } }
        };

        // Appended keys are added to all subsequent log entries in the current execution.
        // Call this method as early as possible in the Lambda handler.
        // Typically this is value would be passed into the function via the event.
        // Set the ClearState = true to force the removal of keys across invocations,
        Logger.AppendKeys(lookupInfo);

        Logger.LogInformation("Getting ip address from external service");

        var location = await GetCallingIp();

        var lookupRecord = new LookupRecord(
            requestContextRequestId,
            "Hello Powertools for AWS Lambda (.NET)",
            location);

        // Trace Fluent API
        Tracing.WithSubsegment(
            "LoggingResponse",
            subsegment =>
            {
                subsegment.AddAnnotation(
                    "AccountId",
                    apigwProxyEvent.RequestContext.AccountId);
                subsegment.AddMetadata(
                    "LookupRecord",
                    lookupRecord);
            });

        try
        {
            await SaveRecordInDynamo(lookupRecord);

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(
                    lookupRecord,
                    typeof(LookupRecord),
                    CustomSerializationContext.Default),
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

    [Tracing(SegmentName = "Location service")]
    private static async Task<string?> GetCallingIp()
    {
        if (_httpClient == null) return "0.0.0.0";
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

        try
        {
            Logger.LogInformation("Calling Check IP API");

            var response = await _httpClient.GetStringAsync("https://checkip.amazonaws.com/").ConfigureAwait(false);
            var ip = response.Replace(
                "\n",
                "");

            Logger.LogInformation($"API response returned {ip}");

            return ip;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    /// <summary>
    /// Saves the lookup record in DynamoDB
    /// </summary>
    /// <param name="lookupRecord"></param>
    /// <returns>A Task that can be used to poll or wait for results, or both.</returns>
    [Tracing(SegmentName = "DynamoDB")]
    private static async Task SaveRecordInDynamo(LookupRecord lookupRecord)
    {
        try
        {
            Logger.LogInformation($"Saving record with id {lookupRecord.LookupId}");
            
            await _dynamoDbClient?.PutItemAsync(Environment.GetEnvironmentVariable("TABLE_NAME"), new Dictionary<string, AttributeValue>(3)
            {
                {"LookupId", new AttributeValue(lookupRecord.LookupId)},
                {"Greeting", new AttributeValue(lookupRecord.Greeting)},
                {"IpAddress", new AttributeValue(lookupRecord.IpAddress)},
            })!;
            
            Metrics.AddMetric("RecordSaved", 1, MetricUnit.Count);
        }
        catch (AmazonDynamoDBException e)
        {
            Logger.LogCritical(e.Message);
            throw;
        }
    }
}