// This file is referenced by docs/core/event_handler/appsync_events.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.AppSyncEvents;

// --8<-- [start:publish_class_library]
using AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

public class Function
{
    AppSyncEventsResolver _app;

    public Function()
    {
        _app = new AppSyncEventsResolver();
        _app.OnPublishAsync("/default/channel", async (payload) =>
        {
            // Handle events or
            // return unchanged payload
            return payload;
        });
    }

    public async Task<AppSyncEventsResponse> FunctionHandler(AppSyncEventsRequest input, ILambdaContext context)
    {
        return await _app.ResolveAsync(input, context);
    }
}
// --8<-- [end:publish_class_library]

// --8<-- [start:publish_executable_assembly]
using AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

var app = new AppSyncEventsResolver();

app.OnPublishAsync("/default/channel", async (payload) =>
{
    // Handle events or
    // return unchanged payload
    return payload;
}

async Task<AppSyncEventsResponse> Handler(AppSyncEventsRequest appSyncEvent, ILambdaContext context)
{
    return await app.ResolveAsync(appSyncEvent, context);
}

await LambdaBootstrapBuilder.Create((Func<AppSyncEventsRequest, ILambdaContext, Task<AppSyncEventsResponse>>)Handler,
new DefaultLambdaJsonSerializer())
.Build()
.RunAsync();
// --8<-- [end:publish_executable_assembly]

// --8<-- [start:subscribe_events]
app.OnSubscribe("/default/*", (payload) =>
{
    // Handle subscribe events
    // return true to allow subscription
    // return false or throw to reject subscription
    return true;
});
// --8<-- [end:subscribe_events]
