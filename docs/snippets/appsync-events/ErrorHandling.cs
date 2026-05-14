// This file is referenced by docs/core/event_handler/appsync_events.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.AppSyncEvents;

// --8<-- [start:error_handling_individual]
app.OnPublish("/default/channel", (payload) =>
{
    throw new Exception("My custom exception");
});
// --8<-- [end:error_handling_individual]

// --8<-- [start:error_handling_individual_async]
app.OnPublishAsync("/default/channel", async (payload) =>
{
    throw new Exception("My custom exception");
});
// --8<-- [end:error_handling_individual_async]

// --8<-- [start:error_handling_batch]
app.OnPublishAggregate("/default/channel", (payload) =>
{
    throw new Exception("My custom exception");
});
// --8<-- [end:error_handling_batch]

// --8<-- [start:error_handling_batch_async]
app.OnPublishAggregateAsync("/default/channel", async (payload) =>
{
    throw new Exception("My custom exception");
});
// --8<-- [end:error_handling_batch_async]

// --8<-- [start:unauthorized_exception]
app.OnPublish("/default/channel", (payload) =>
{
    throw new UnauthorizedException("My custom exception");
});
// --8<-- [end:unauthorized_exception]
