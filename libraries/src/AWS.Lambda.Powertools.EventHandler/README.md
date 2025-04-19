# AWS Lambda Powertools for .NET - Event Handler

## AppSync Events

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
    return await app.Resolve(appSyncEvent, context);
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

app.OnPublish("/default/channel", async (payload) =>
{
    Logger.LogInformation("Published to /default/channel with {@payload}", payload);

    if (payload["eventType"].ToString() == "data_2")
    {
        throw new Exception("Error in /default/channel");
    }

    return "Hello from /default/channel";
});

app.OnPublishAggregate("/default/channel2", async (payload) =>
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

app.OnSubscribe("/default/*", async (payload) =>
{
    Logger.LogInformation("Subscribed to /default/* with {@payload}", payload);
    return await Task.FromResult(true);
});

async Task<AppSyncEventsResponse> Handler(AppSyncEventsRequest appSyncEvent, ILambdaContext context)
{
    return await app.Resolve(appSyncEvent, context);
}

await LambdaBootstrapBuilder.Create((Func<AppSyncEventsRequest, ILambdaContext, Task<AppSyncEventsResponse>>)Handler,
new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();
```