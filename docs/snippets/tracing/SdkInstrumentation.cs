// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing;

// --8<-- [start:register_all_services]
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    private static IAmazonDynamoDB _dynamoDb;

    /// <summary>
    /// Function constructor
    /// </summary>
    public Function()
    {
        Tracing.RegisterForAllServices();

        _dynamoDb = new AmazonDynamoDBClient();
    }
}
// --8<-- [end:register_all_services]

// --8<-- [start:register_single_service]
Tracing.Register<IAmazonDynamoDB>()
// --8<-- [end:register_single_service]

// --8<-- [start:instrument_http_calls]
using Amazon.XRay.Recorder.Handlers.System.Net;

public class Function
{
    public Function()
    {
        var httpClient = new HttpClient(new HttpClientXRayTracingHandler(new HttpClientHandler()));
        var myIp = await httpClient.GetStringAsync("https://checkip.amazonaws.com/");
    }
}
// --8<-- [end:instrument_http_calls]
