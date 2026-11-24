// This file is referenced by docs/core/event_handler/appsync_events.md
// via pymdownx.snippets (mkdocs).

using AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

namespace Docs.AppSyncEventsSnippets.AdvancedUsage
{
internal class Example
{
    private readonly AppSyncEventsResolver app = new();

    public void Register()
    {

// --8<-- [start:accessing_lambda_context]
app.OnPublish("/default/channel", (payload, ctx) =>
{
    payload["functionName"] = ctx.FunctionName;
    return payload;
});
// --8<-- [end:accessing_lambda_context]
    }
}
}
