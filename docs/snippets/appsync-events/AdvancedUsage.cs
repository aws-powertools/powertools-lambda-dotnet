// This file is referenced by docs/core/event_handler/appsync_events.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.AppSyncEvents;

// --8<-- [start:accessing_lambda_context]
app.OnPublish("/default/channel", (payload, ctx) =>
{
    payload["functionName"] = ctx.FunctionName;
    return payload;
});
// --8<-- [end:accessing_lambda_context]
