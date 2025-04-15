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

        app.OnPublish("/default/channel", async (payload) =>
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
    public async Task Should_Process_Events_In_Aggregate()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();
        
        app.OnPublish("/default/channel", async (evt, ctx) =>
        {
            // Create aggregate result from all events
            return new Dictionary<string, object>
            {
                ["combined"] = true,
                ["count"] = evt.Events.Count(),
                ["ids"] = string.Join(",", evt.Events.Select(e => e.id))
            };
        }, aggregate: true);

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Single(result.Events);
        Assert.Equal("3", result.Events[0].Payload["count"].ToString());
        Assert.Equal("1,2,3", result.Events[0].Payload["ids"].ToString());
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

        app.OnSubscribe("/default/*", async (info) => 
        {
            throw new Exception("Authorization error");
        });
        
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
    public async Task Should_Execute_Multiple_Matching_Handlers()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();
        
        app.OnPublish("/default/channel", async (payload) => 
        {
            return new Dictionary<string, object> { ["handler"] = "first" };
        });
        
        app.OnPublish("/default/*", async (payload) => 
        {
            return new Dictionary<string, object> { ["handler"] = "second" };
        });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(6, result.Events.Count);
        Assert.Equal("first", result.Events[0].Payload["handler"].ToString());
        Assert.Equal("first", result.Events[1].Payload["handler"].ToString());
        Assert.Equal("second", result.Events[4].Payload["handler"].ToString());
        Assert.Equal("second", result.Events[5].Payload["handler"].ToString());
    }
    
    [Fact]
    public async Task Should_Respect_HandlerPathSpecificity()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/*", async (payload) =>
        {
            return new Dictionary<string, object> { ["handler"] = "least-specific" };
        });

        app.OnPublish("/default/*", async (payload) =>
        {
            return new Dictionary<string, object> { ["handler"] = "more-specific" };
        });

        app.OnPublish("/default/channel", async (payload) =>
        {
            return new Dictionary<string, object> { ["handler"] = "most-specific" };
        });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert - The most specific handler should be first
        Assert.Equal(9, result.Events.Count); // 3 handlers x 3 events
        Assert.Equal("most-specific", result.Events[0].Payload["handler"].ToString());
        Assert.Equal("more-specific", result.Events[3].Payload["handler"].ToString());
        Assert.Equal("least-specific", result.Events[6].Payload["handler"].ToString());
    }

    [Fact]
    public async Task Should_Handle_Error_In_Aggregate_Mode()
    {
        // Arrange
        var lambdaContext = new TestLambdaContext();
        var app = new AppSyncEventsResolver();

        app.OnPublish("/default/channel", async (evt, ctx) =>
        {
            throw new InvalidOperationException("Aggregate error");
        }, aggregate: true);

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

        app.OnPublish("/default/channel", async (payload) =>
        {
            return new Dictionary<string, object> { ["handler"] = "first" };
        });

        app.OnPublish("/default/channel", async (payload) =>
        {
            return new Dictionary<string, object> { ["handler"] = "second" };
        });

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

        app.OnPublish("/default/channel", async (payload) =>
        {
            return new Dictionary<string, object> { ["processed"] = true };
        });

        // Act
        var result = await app.Resolve(_appSyncEvent, lambdaContext);

        // Assert
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("1", result.Events[0].Id);
        Assert.Equal("2", result.Events[1].Id);
        Assert.Equal("3", result.Events[2].Id);
    }
    
}