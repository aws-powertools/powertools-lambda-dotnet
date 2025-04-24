# AWS Lambda Powertools for .NET - Event Handler

## Event Handler for AWS AppSync real-time events.

## Key Features

* Easily handle publish and subscribe events with dedicated handler methods
* Automatic routing based on namespace and channel patterns
* Support for wildcard patterns to create catch-all handlers
* Process events in parallel or sequentially
* Control over event aggregation for batch processing
* Graceful error handling for individual events

## Terminology

**[AWS AppSync Events](https://docs.aws.amazon.com/appsync/latest/eventapi/event-api-welcome.html){target="_blank"}**. A service that enables you to quickly build secure, scalable real-time WebSocket APIs without managing infrastructure or writing API code. It handles connection management, message broadcasting, authentication, and monitoring, reducing time to market and operational costs.

### Getting Started

1. Install the NuGet package:

```bash
dotnet add package AWS.Lambda.Powertools.EventHandler --version 1.0.0
```
2. Add the `AWS.Lambda.Powertools.EventHandler` namespace to your Lambda function:

```csharp
using AWS.Lambda.Powertools.EventHandler;
```
3. Update the AWS Lambda handler to use `AppSyncEventsResolver`

```csharp
async Task<AppSyncEventsResponse> Handler(AppSyncEventsRequest appSyncEvent, ILambdaContext context)
{
    return await app.ResolveAsync(appSyncEvent, context);
}
```

### Example

```csharp
using AWS.Lambda.Powertools.EventHandler;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.EventHandler.AppSyncEvents;
using AWS.Lambda.Powertools.Logging;

var app = new AppSyncEventsResolver();

app.OnPublishAsync("/default/channel", async (payload) =>
{
    Logger.LogInformation("Published to /default/channel with {@payload}", payload);

    if (payload["eventType"].ToString() == "data_2")
    {
        throw new Exception("Error in /default/channel");
    }

    return "Hello from /default/channel";
});

app.OnPublishAggregateAsync("/default/channel2", async (payload) =>
{
    var evt = new List<AppSyncEvent>();
    foreach (var item in payload.Events)
    {
        var pd = new AppSyncEvent
        {
            Id = item.Id,
            Payload = new Dictionary<string, object>
            {
                { "demo", "demo" }
            }
        };

        if (item.Payload["eventType"].ToString() == "data_2")
        {
            pd.Payload["message"] = "Hello from /default/channel2 with data_2";
            pd.Payload["data"] = new Dictionary<string, object>
            {
                { "key", "value" }
            };
        }

        evt.Add(pd);
    }

    Logger.LogInformation("Published to /default/channel2 with {@evt}", evt);
    return new AppSyncEventsResponse
    {
        Events = evt
    };
});

app.OnSubscribeAsync("/default/*", async (payload) =>
{
    Logger.LogInformation("Subscribed to /default/* with {@payload}", payload);
    return true;
});

async Task<AppSyncEventsResponse> Handler(AppSyncEventsRequest appSyncEvent, ILambdaContext context)
{
    return await app.ResolveAsync(appSyncEvent, context);
}

await LambdaBootstrapBuilder.Create((Func<AppSyncEventsRequest, ILambdaContext, Task<AppSyncEventsResponse>>)Handler,
new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();
```