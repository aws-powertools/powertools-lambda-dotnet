using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler.Internal;

namespace AWS.Lambda.Powertools.EventHandler.AppSyncEvents;

/// <summary>
/// Resolver for AWS AppSync Events APIs.
/// Handles onPublish and onSubscribe events from AppSync Events APIs,
/// routing them to appropriate handlers based on path.
/// </summary>
public class AppSyncEventsResolver
{
    private readonly RouteHandlerRegistry<AppSyncResolverEvent, object> _publishRoutes;
    private readonly RouteHandlerRegistry<AppSyncResolverEvent, bool> _subscribeRoutes;

    public AppSyncEventsResolver()
    {
        _publishRoutes = new RouteHandlerRegistry<AppSyncResolverEvent, object>();
        _subscribeRoutes = new RouteHandlerRegistry<AppSyncResolverEvent, bool>();
    }

    /// <summary>
    /// Registers a handler for publish events on a specific channel path.
    /// Processes each event in the payload individually.
    /// </summary>
    public AppSyncEventsResolver OnPublish(string path, Func<Dictionary<string, object>, Task<object>> handler)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncResolverEvent, object>
        {
            Path = path,
            Handler = (evt, ctx) =>
            {
                var payload = evt.Events.FirstOrDefault()?.Payload;
                return handler(payload ?? new Dictionary<string, object>());
            },
            Aggregate = false
        });
        return this;
    }

    /// <summary>
    /// Registers a handler for publish events on a specific channel path.
    /// Processes each event in the payload individually.
    /// Lambda context available
    /// </summary>
    public AppSyncEventsResolver OnPublish(string path,
        Func<Dictionary<string, object>, ILambdaContext, Task<object>> handler)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncResolverEvent, object>
        {
            Path = path,
            Handler = (evt, ctx) =>
            {
                var payload = evt.Events.FirstOrDefault()?.Payload;
                return handler(payload ?? new Dictionary<string, object>(), ctx);
            },
            Aggregate = false
        });
        return this;
    }

    /// <summary>
    /// Registers a handler for publish events on a specific channel path.
    /// Processes all events in a single handler invocation.
    /// </summary>
    public AppSyncEventsResolver OnPublish(string path, Func<AppSyncResolverEvent, Task<object>> handler,
        bool aggregate = true)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncResolverEvent, object>
        {
            Path = path,
            Handler = (evt, ctx) => handler(evt),
            Aggregate = aggregate
        });
        return this;
    }

    /// <summary>
    /// Registers a handler for publish events on a specific channel path.
    /// Processes all events in a single handler invocation.
    /// Lambda context available
    /// </summary>
    public AppSyncEventsResolver OnPublish(string path,
        Func<AppSyncResolverEvent, ILambdaContext, Task<object>> handler, bool aggregate = true)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncResolverEvent, object>
        {
            Path = path,
            Handler = handler,
            Aggregate = aggregate
        });
        return this;
    }

    /// <summary>
    /// Registers a handler for subscription events on a specific channel path.
    /// </summary>
    public AppSyncEventsResolver OnSubscribe(string path, Func<Information, Task<bool>> handler)
    {
        _subscribeRoutes.Register(new RouteHandlerOptions<AppSyncResolverEvent, bool>
        {
            Path = path,
            Handler = async (evt, ctx) => await handler(ExtractSubscriptionInfo(evt)),
            Aggregate = true
        });
        return this;
    }

    /// <summary>
    /// Registers a handler for subscription events on a specific channel path with Lambda context.
    /// </summary>
    public AppSyncEventsResolver OnSubscribe(string path, Func<Information, ILambdaContext, Task<bool>> handler)
    {
        _subscribeRoutes.Register(new RouteHandlerOptions<AppSyncResolverEvent, bool>
        {
            Path = path,
            Handler = async (evt, ctx) => await handler(ExtractSubscriptionInfo(evt), ctx),
            Aggregate = true
        });
        return this;
    }

    public async Task<AppSyncResolverEventsResponse> Resolve(AppSyncResolverEvent appsyncEvent, ILambdaContext context)
    {
        if (IsPublishEvent(appsyncEvent))
        {
            return await HandlePublishEvent(appsyncEvent, context);
        }

        if (IsSubscribeEvent(appsyncEvent))
        {
            return await HandleSubscribeEvent(appsyncEvent, context);
        }

        throw new InvalidOperationException("Unknown event type");
    }

    private async Task<AppSyncResolverEventsResponse> HandlePublishEvent(AppSyncResolverEvent appsyncEvent,
        ILambdaContext context)
    {
        var channelPath = appsyncEvent.Info.Channel.Path;
        var handlerOptions = _publishRoutes.ResolveFirst(channelPath);

        if (handlerOptions == null)
        {
            // Return unchanged events if no handler found
            var events = appsyncEvent.Events
                .Select(e => new AppSyncResolverEventsResult
                {
                    Id = e.Id,
                    Payload = e.Payload
                })
                .ToList();
            return new AppSyncResolverEventsResponse { Events = events };
        }

        var results = new List<AppSyncResolverEventsResult>();

        if (handlerOptions.Aggregate)
        {
            try
            {
                // Process entire event in one call
                var handlerResult = await handlerOptions.Handler(appsyncEvent, context);

                // Check if the handler returned a collection of events
                if (handlerResult is Dictionary<string, object> dict &&
                    dict.TryGetValue("events", out var eventsObj) &&
                    eventsObj is IEnumerable<object> eventsList)
                {
                    // Process each event in the collection
                    foreach (var eventObj in eventsList)
                    {
                        if (eventObj is Dictionary<string, object> eventDict)
                        {
                            string eventId = null;
                            if (eventDict.TryGetValue("id", out var idObj) && idObj != null)
                            {
                                eventId = idObj.ToString();
                            }

                            if (eventDict.TryGetValue("error", out var errorObj) && errorObj != null)
                            {
                                // This is an error result
                                results.Add(new AppSyncResolverEventsResult
                                {
                                    Id = eventId,
                                    Error = errorObj.ToString()
                                });
                            }
                            else
                            {
                                // Remove id from payload if present
                                var payload = new Dictionary<string, object>(eventDict);
                                if (payload.ContainsKey("id")) payload.Remove("id");

                                results.Add(new AppSyncResolverEventsResult
                                {
                                    Id = eventId,
                                    Payload = payload
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(FormatErrorResponse(ex, null));
            }
        }
        else
        {
            // Process each event individually
            foreach (var eventItem in appsyncEvent.Events)
            {
                try
                {
                    // Create a copy of the event with just this single event
                    var singleEventCopy = new AppSyncResolverEvent
                    {
                        Info = appsyncEvent.Info,
                        Events = [eventItem]
                    };

                    var handlerResult = await handlerOptions.Handler(singleEventCopy, context);
                    var payload = ConvertToPayload(handlerResult, out var error);

                    results.Add(new AppSyncResolverEventsResult
                    {
                        Id = eventItem.Id,
                        Payload = payload,
                        Error = error
                    });
                }
                catch (Exception ex)
                {
                    results.Add(FormatErrorResponse(ex, eventItem.Id));
                }
            }
        }

        return new AppSyncResolverEventsResponse { Events = results };
    }

    private async Task<AppSyncResolverEventsResponse> HandleSubscribeEvent(AppSyncResolverEvent appsyncEvent,
        ILambdaContext context)
    {
        var channelPath = appsyncEvent.Info.Channel.Path;
        var handlerOptions = _subscribeRoutes.ResolveFirst(channelPath);

        if (handlerOptions == null)
        {
            return new AppSyncResolverEventsResponse { Authorized = false };
        }

        try
        {
            var result = await handlerOptions.Handler(appsyncEvent, context);
            return new AppSyncResolverEventsResponse { Authorized = (bool)result };
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error in subscribe handler: {ex.Message}");
            return new AppSyncResolverEventsResponse { Authorized = false };
        }
    }

    private Information ExtractSubscriptionInfo(AppSyncResolverEvent appsyncEvent)
    {
        return new Information
        {
            Channel = new Channel
            {
                Path = appsyncEvent.Info.Channel.Path
            }
        };
    }

    private Dictionary<string, object> ConvertToPayload(object result, out string error)
    {
        error = null;

        // Check if this is an error result from ProcessSingleEvent
        if (result is Dictionary<string, object> dict && dict.ContainsKey("error"))
        {
            error = dict["error"].ToString();
            return null; // No payload when there's an error
        }

        // Regular payload handling
        if (result is Dictionary<string, object> payload)
        {
            return payload;
        }

        return new Dictionary<string, object> { ["data"] = result };
    }

    private AppSyncResolverEventsResult FormatErrorResponse(Exception ex, string id)
    {
        return new AppSyncResolverEventsResult
        {
            Id = id, // This will be the original event ID or null
            Error = $"{ex.GetType().Name} - {ex.Message}"
        };
    }

    private bool IsPublishEvent(AppSyncResolverEvent appsyncEvent)
    {
        return appsyncEvent.Info.Operation == AppsyncEventsOperation.Publish;
    }

    private bool IsSubscribeEvent(AppSyncResolverEvent appsyncEvent)
    {
        return appsyncEvent.Info.Operation == AppsyncEventsOperation.Subscribe;
    }
}