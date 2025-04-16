using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

namespace AWS.Lambda.Powertools.EventHandler.Tests;

public class AppSyncEventsTests
{
    private readonly AppSyncResolverEvent? _appSyncEvent;

    public AppSyncEventsTests()
    {
        _appSyncEvent = JsonSerializer.Deserialize<AppSyncResolverEvent>(
            File.ReadAllText("appSyncEventsEvent.json"),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });
    }

    [Fact]
    public async Task Should_Return_Unchanged_Payload_No_Handlers()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        // Act
        var result =
            await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("1", result.Events[0].Id);
        Assert.Equal("data_1", result.Events[0].Payload?["event_1"].ToString());
        Assert.Equal("2", result.Events[1].Id);
        Assert.Equal("data_2", result.Events[1].Payload?["event_2"].ToString());
        Assert.Equal("3", result.Events[2].Id);
        Assert.Equal("data_3", result.Events[2].Payload?["event_3"].ToString());
    }

    [Fact]
    public async Task Should_Return_Unchanged_Payload()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/default/channel", async (Dictionary<string, object> payload) =>
        {
            // Handle channel1 events
            return payload;
        });

        // Act
        var result =
            await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("1", result.Events[0].Id);
        Assert.Equal("data_1", result.Events[0].Payload?["event_1"].ToString());
        Assert.Equal("2", result.Events[1].Id);
        Assert.Equal("data_2", result.Events[1].Payload?["event_2"].ToString());
        Assert.Equal("3", result.Events[2].Id);
        Assert.Equal("data_3", result.Events[2].Payload?["event_3"].ToString());
    }

    [Fact]
    public async Task Should_Handle_Error_In_Event_Processing()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/default/channel", async (payload) =>
        {
            // Throw exception for second event
            if (payload.ContainsKey("event_2"))
            {
                throw new InvalidOperationException("Test error");
            }

            return payload;
        });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("1", result.Events[0].Id);
        Assert.Equal("data_1", result.Events[0].Payload["event_1"].ToString());
        Assert.Equal("2", result.Events[1].Id);
        Assert.NotNull(result.Events[1].Error);
        Assert.Contains("Test error", result.Events[1].Error);
        Assert.Equal("3", result.Events[2].Id);
        Assert.Equal("data_3", result.Events[2].Payload["event_3"].ToString());
    }

    [Fact]
    public async Task Should_Match_Path_With_Wildcard()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        int callCount = 0;
        app.OnPublish("/default/*", async (payload) =>
        {
            callCount++;
            return new Dictionary<string, object> { ["wildcard_matched"] = true };
        });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal(3, callCount);
        Assert.True((bool)result.Events[0].Payload["wildcard_matched"]);
    }

    [Fact]
    public async Task Should_Authorize_Subscription()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnSubscribe("/default/*", async (info) => true);
        var subscribeEvent = new AppSyncResolverEvent
        {
            Info = new Information
            {
                Channel = new Channel { Path = "/default/channel" },
                Operation = AppsyncEventsOperation.Subscribe
            }
        };
        // Act
        var result = await app.Resolve(subscribeEvent, lambdaContext);

        // Assert
        Assert.True(result.Authorized);
    }

    [Fact]
    public async Task Should_Deny_Subscription()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnSubscribe("/default/*", async (info) => false);
        var subscribeEvent = new AppSyncResolverEvent
        {
            Info = new Information
            {
                Channel = new Channel { Path = "/default/channel" },
                Operation = AppsyncEventsOperation.Subscribe
            }
        };
        // Act
        var result = await app.Resolve(subscribeEvent, lambdaContext);

        // Assert
        Assert.False(result.Authorized);
    }

    [Fact]
    public async Task Should_Deny_Subscription_On_Exception()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnSubscribe("/default/*", async (info) => { throw new Exception("Authorization error"); });

        var subscribeEvent = new AppSyncResolverEvent
        {
            Info = new Information
            {
                Channel = new Channel { Path = "/default/channel" },
                Operation = AppsyncEventsOperation.Subscribe
            }
        };

        // Act
        var result = await app.Resolve(subscribeEvent, lambdaContext);

        // Assert
        Assert.False(result.Authorized);
    }

    [Fact]
    public async Task Should_Handle_Error_In_Aggregate_Mode()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/default/channel",
            async (evt, ctx) => { throw new InvalidOperationException("Aggregate error"); }, aggregate: true);

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Single(result.Events);
        Assert.NotNull(result.Events[0].Error);
        Assert.Contains("Aggregate error", result.Events[0].Error);
    }

    [Fact]
    public async Task Should_Handle_TransformingPayload()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/default/channel", async (payload) =>
        {
            // Transform each event payload
            var transformedPayload = new Dictionary<string, object>();
            foreach (var key in payload.Keys)
            {
                transformedPayload[$"transformed_{key}"] = $"transformed_{payload[key]}";
            }

            return transformedPayload;
        });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("transformed_event_1", result.Events[0].Payload.Keys.First());
        Assert.Equal("transformed_data_1", result.Events[0].Payload["transformed_event_1"].ToString());
    }

    [Fact]
    public async Task Should_Throw_For_Unknown_EventType()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        var unknownEvent = new AppSyncResolverEvent
        {
            Info = new Information
            {
                Channel = new Channel { Path = "/default/channel" },
                Operation = (AppsyncEventsOperation)999 // Unknown operation
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            app.Resolve(unknownEvent, lambdaContext));
    }

    [Fact]
    public async Task Should_Return_NonDictionary_Values_Wrapped_In_Data()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/default/channel", async (payload) =>
        {
            // Return a non-dictionary value
            return "string value";
        });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("string value", result.Events[0].Payload["data"].ToString());
    }

    [Fact]
    public async Task Should_Skip_Invalid_Path_Registration()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();
        var handlerCalled = false;

        // Register with invalid path
        app.OnPublish("/invalid/*/path", async (payload) =>
        {
            handlerCalled = true;
            return payload;
        });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert - Should return original payload, handler not called
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("data_1", result.Events[0].Payload["event_1"].ToString());
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task Should_Replace_Handler_When_RegisteringTwice()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/default/channel",
            async (payload) => { return new Dictionary<string, object> { ["handler"] = "first" }; });

        app.OnPublish("/default/channel",
            async (payload) => { return new Dictionary<string, object> { ["handler"] = "second" }; });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert - Only second handler should be used
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("second", result.Events[0].Payload["handler"].ToString());
    }

    [Fact]
    public async Task Should_Maintain_EventIds_When_Processing()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/default/channel",
            async (payload) => { return new Dictionary<string, object> { ["processed"] = true }; });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("1", result.Events[0].Id);
        Assert.Equal("2", result.Events[1].Id);
        Assert.Equal("3", result.Events[2].Id);
    }

    [Fact]
    public async Task Aggregate_Handler_Can_Return_Individual_Results_With_Ids()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/default/channel", async (evt) =>
        {
            // Iterate through events and return individual results with IDs
            var results = new List<Dictionary<string, object>>();

            foreach (var eventItem in evt.Events)
            {
                try
                {
                    if (eventItem.Payload.ContainsKey("event_2"))
                    {
                        // Create an error for the second event
                        results.Add(new Dictionary<string, object>
                        {
                            ["id"] = eventItem.Id,
                            ["error"] = "Intentional error for event 2"
                        });
                    }
                    else
                    {
                        // Process normally
                        results.Add(new Dictionary<string, object>
                        {
                            ["id"] = eventItem.Id,
                            ["processed"] = true,
                            ["originalData"] = eventItem.Payload
                        });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new Dictionary<string, object>
                    {
                        ["id"] = eventItem.Id,
                        ["error"] = $"{ex.GetType().Name} - {ex.Message}"
                    });
                }
            }

            return new Dictionary<string, object> { ["events"] = results };
        }, aggregate: true);

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("1", result.Events[0].Id);
        Assert.True((bool)result.Events[0].Payload["processed"]);
        Assert.Equal("2", result.Events[1].Id);
        Assert.NotNull(result.Events[1].Error);
        Assert.Contains("Intentional error for event 2", result.Events[1].Error);
        Assert.Equal("3", result.Events[2].Id);
        Assert.True((bool)result.Events[2].Payload["processed"]);
    }

    [Fact]
    public async Task Should_Verify_Ids_Are_Preserved_In_Error_Case()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        // Create handlers that throw exceptions for specific events
        app.OnPublish("/default/channel", async (payload) =>
        {
            if (payload.ContainsKey("event_1"))
                throw new InvalidOperationException("Error for event 1");
            if (payload.ContainsKey("event_3"))
                throw new ArgumentException("Error for event 3");
            return payload;
        });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("1", result.Events[0].Id);
        Assert.Contains("Error for event 1", result.Events[0].Error);
        Assert.Equal("2", result.Events[1].Id);
        Assert.Null(result.Events[1].Error);
        Assert.Equal("3", result.Events[2].Id);
        Assert.Contains("Error for event 3", result.Events[2].Error);
    }
    
    [Fact]
public async Task Should_Match_Most_Specific_Handler_Only()
{
    // Arrange
    var lambdaContext = new TestLambdaContext();
    var app = new AppSyncEventsResolver();
    
    int firstHandlerCalls = 0;
    int secondHandlerCalls = 0;

    app.OnPublish("/default/channel", async (payload) => 
    { 
        firstHandlerCalls++;
        return new Dictionary<string, object> { ["handler"] = "first" }; 
    });

    app.OnPublish("/default/*", async (payload) => 
    { 
        secondHandlerCalls++;
        return new Dictionary<string, object> { ["handler"] = "second" }; 
    });

    // Act
    var result = await app.Resolve(_appSyncEvent, lambdaContext);

    // Assert - Only the first (most specific) handler should be called
    Assert.Equal(3, result.Events.Count);
    Assert.Equal("first", result.Events[0].Payload["handler"].ToString());
    Assert.Equal(3, firstHandlerCalls);
    Assert.Equal(0, secondHandlerCalls);
}

[Fact]
public async Task Should_Handle_Multiple_Keys_In_Payload()
{
    // Arrange
    var lambdaContext = new TestLambdaContext();
    var app = new AppSyncEventsResolver();

    // Create an event with multiple keys in the payload
    var multiKeyEvent = new AppSyncResolverEvent
    {
        Info = new Information
        {
            Channel = new Channel { Path = "/default/channel" },
            Operation = AppsyncEventsOperation.Publish
        },
        Events =
        [
            new Event
            {
                Id = "1",
                Payload = new Dictionary<string, object>
                {
                    ["event_1"] = "data_1",
                    ["event_1a"] = "data_1a"
                }
            }
        ]
    };

    app.OnPublish("/default/channel", async (payload) =>
    {
        // Check that both keys are present
        Assert.Equal("data_1", payload["event_1"]);
        Assert.Equal("data_1a", payload["event_1a"]);
        
        // Return a processed result with both keys
        return new Dictionary<string, object> 
        { 
            ["processed_1"] = payload["event_1"],
            ["processed_1a"] = payload["event_1a"]
        };
    });

    // Act
    var result = await app.Resolve(multiKeyEvent, lambdaContext);

    // Assert
    Assert.Single(result.Events);
    Assert.Equal("1", result.Events[0].Id);
    Assert.Equal("data_1", result.Events[0].Payload["processed_1"]);
    Assert.Equal("data_1a", result.Events[0].Payload["processed_1a"]);
}

[Fact]
public async Task Should_Only_Use_First_Matching_Handler_By_Specificity()
{
    // Arrange
    var lambdaContext = new TestLambdaContext();
    var app = new AppSyncEventsResolver();

    // Register handlers with different specificity
    app.OnPublish("/*", async (payload) => 
        new Dictionary<string, object> { ["handler"] = "least-specific" });

    app.OnPublish("/default/*", async (payload) => 
        new Dictionary<string, object> { ["handler"] = "more-specific" });

    app.OnPublish("/default/channel", async (payload) => 
        new Dictionary<string, object> { ["handler"] = "most-specific" });

    // Act
    var result = await app.Resolve(_appSyncEvent, lambdaContext);

    // Assert - Only the most specific handler should be called
    Assert.Equal(3, result.Events.Count);
    Assert.Equal("most-specific", result.Events[0].Payload["handler"].ToString());
    Assert.Equal("most-specific", result.Events[1].Payload["handler"].ToString());
    Assert.Equal("most-specific", result.Events[2].Payload["handler"].ToString());
}

[Fact]
public async Task Should_Fallback_To_Less_Specific_Handler_If_No_Exact_Match()
{
    // Arrange
    var lambdaContext = new TestLambdaContext();
    var app = new AppSyncEventsResolver();
    
    // Create an event with a path that has no exact match
    var fallbackEvent = new AppSyncResolverEvent
    {
        Info = new Information
        {
            Channel = new Channel { Path = "/default/specific/path" },
            Operation = AppsyncEventsOperation.Publish
        },
        Events =
        [
            new Event
            {
                Id = "1",
                Payload = new Dictionary<string, object> { ["key"] = "value" }
            }
        ]
    };

    app.OnPublish("/default/*", async (payload) => 
        new Dictionary<string, object> { ["handler"] = "wildcard-handler" });

    // Act
    var result = await app.Resolve(fallbackEvent, lambdaContext);

    // Assert
    Assert.Single(result.Events);
    Assert.Equal("wildcard-handler", result.Events[0].Payload["handler"].ToString());
}
}