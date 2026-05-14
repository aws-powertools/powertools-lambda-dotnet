// This file is referenced by docs/core/event_handler/appsync_events.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.AppSyncEvents;

// --8<-- [start:test_publish_events]
[Fact]
public void Should_Return_Unchanged_Payload()
{
    // Arrange
    var lambdaContext = new TestLambdaContext();
    var app = new AppSyncEventsResolver();

    app.OnPublish("/default/channel", payload =>
    {
        // Handle channel events
        return payload;
    });

    // Act
    var result = app.Resolve(_appSyncEvent, lambdaContext);

    // Assert
    Assert.Equal("123", result.Events[0].Id);
    Assert.Equal("test data", result.Events[0].Payload?["data"].ToString());
}
// --8<-- [end:test_publish_events]

// --8<-- [start:test_subscribe_events]
[Fact]
public async Task Should_Authorize_Subscription()
{
    // Arrange
    var lambdaContext = new TestLambdaContext();
    var app = new AppSyncEventsResolver();

    app.OnSubscribeAsync("/default/*", async (info) => true);

    var subscribeEvent = new AppSyncEventsRequest
    {
        Info = new Information
        {
            Channel = new Channel
            {
                Path = "/default/channel",
                Segments = ["default", "channel"]
            },
            Operation = AppSyncEventsOperation.Subscribe,
            ChannelNamespace = new ChannelNamespace { Name = "default" }
        }
    };
    // Act
    var result = await app.ResolveAsync(subscribeEvent, lambdaContext);

    // Assert
    Assert.Null(result);
}
// --8<-- [end:test_subscribe_events]
