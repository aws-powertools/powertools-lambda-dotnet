// This file is referenced by docs/core/event_handler/appsync_events.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.AppSyncEvents;

// --8<-- [start:aggregated_processing]
app.OnPublishAggregate("/default/channel", (payload) =>
{
    var evt = new List<AppSyncEvent>();

    foreach (var item in payload.Events)
    {
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

    return new AppSyncEventsResponse
    {
        Events = evt
    };
});
// --8<-- [end:aggregated_processing]
