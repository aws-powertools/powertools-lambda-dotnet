// This file is referenced by docs/core/event_handler/appsync_events.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.AppSyncEvents;

// --8<-- [start:wildcard_patterns]
app.OnPublish("/default/channel1", (payload) =>
{
    // This handler will be called for events on /default/channel1
    return payload;
});

app.OnPublish("/default/*", (payload) =>
{
    // This handler will be called for all channels in the default namespace
    // EXCEPT for /default/channel1 which has a more specific handler
    return payload;
});

app.OnPublish("/*", (payload) =>
{
    // This handler will be called for all channels in all namespaces
    // EXCEPT for those that have more specific handlers
    return payload;
});
// --8<-- [end:wildcard_patterns]
