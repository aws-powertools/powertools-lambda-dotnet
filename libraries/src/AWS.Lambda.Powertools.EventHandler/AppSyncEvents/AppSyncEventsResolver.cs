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
    /// Registers a handler for publish events on a specific channel path
    /// Processes each event in the payload individually
    /// </summary>
    public AppSyncEventsResolver OnPublish(string path, Func<Dictionary<string, object>, Task<object>> handler,
        bool aggregate = false)
    {
        return OnPublish(path, async (evt, context) =>
        {
            var tasks = evt.Events.Select(eventItem =>
                ProcessSingleEvent(eventItem.Payload, handler)
            ).ToList();

            var results = await Task.WhenAll(tasks);
            return results;
        }, aggregate);
    }

    public AppSyncEventsResolver OnPublish(string path,
        Func<AppSyncResolverEvent, ILambdaContext, Task<object>> handler, bool aggregate = false)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncResolverEvent, object>
        {
            Path = path,
            Handler = handler,
            Aggregate = aggregate
        });

        return this;
    }

    public AppSyncEventsResolver OnSubscribe(string path, Func<Information, Task<bool>> handler)
    {
        return OnSubscribe(path, (evt, _) => handler(ExtractSubscriptionInfo(evt)));
    }

    public AppSyncEventsResolver OnSubscribe(string path,
        Func<AppSyncResolverEvent, ILambdaContext, Task<bool>> handler)
    {
        _subscribeRoutes.Register(new RouteHandlerOptions<AppSyncResolverEvent, bool>
        {
            Path = path,
            Handler = handler
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
        var matchingHandlers = _publishRoutes.ResolveAll(channelPath);

        if (matchingHandlers.Count == 0)
        {
            return new AppSyncResolverEventsResponse
            {
                Events = appsyncEvent.Events.Select(e =>
                    new AppSyncResolverEventsResult
                    {
                        Id = e.id,
                        Payload = e.Payload
                    }).ToList()
            };
        }

        var results = new List<AppSyncResolverEventsResult>();

        // Process each matching handler
        foreach (var handlerOptions in matchingHandlers)
        {
            try
            {
                var handler = handlerOptions.Handler;
                var aggregate = handlerOptions.Aggregate;

                // Call handler once per registered handler
                var handlerResult = await handler(appsyncEvent, context);

                if (aggregate)
                {
                    // For aggregate mode, return a single result
                    string error;
                    var payload = ConvertToPayload(handlerResult, out error);
                    results.Add(new AppSyncResolverEventsResult
                    {
                        Id = Guid.NewGuid().ToString(),
                        Payload = payload,
                        Error = error
                    });
                }
                else if (handlerResult is IEnumerable<object> resultArray)
                {
                    // For non-aggregate mode with array result
                    var eventItems = appsyncEvent.Events.ToArray();
                    var i = 0;

                    foreach (var item in resultArray)
                    {
                        var id = i < eventItems.Length ? eventItems[i].id : Guid.NewGuid().ToString();

                        // Convert payload and check for errors
                        string errorMessage;
                        var payload = ConvertToPayload(item, out errorMessage);

                        if (errorMessage != null)
                        {
                            results.Add(new AppSyncResolverEventsResult
                            {
                                Id = id,
                                Error = errorMessage
                            });
                        }
                        else
                        {
                            results.Add(new AppSyncResolverEventsResult
                            {
                                Id = id,
                                Payload = payload
                            });
                        }

                        i++;
                    }
                }
                else
                {
                    string error;
                    var payload = ConvertToPayload(handlerResult, out error);
                    results.Add(new AppSyncResolverEventsResult
                    {
                        Id = appsyncEvent.Events.FirstOrDefault()?.id ?? Guid.NewGuid().ToString(),
                        Payload = payload,
                        Error = error
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(FormatErrorResponse(ex, Guid.NewGuid().ToString()));
            }
        }

        return new AppSyncResolverEventsResponse { Events = results };
    }

    private async Task<AppSyncResolverEventsResponse> HandleSubscribeEvent(AppSyncResolverEvent appsyncEvent,
        ILambdaContext context)
    {
        var channelPath = appsyncEvent.Info.Channel.Path;
        var matchingHandlers = _subscribeRoutes.ResolveAll(channelPath);

        if (matchingHandlers.Count == 0)
        {
            return new AppSyncResolverEventsResponse { Authorized = false };
        }

        try
        {
            foreach (var handlerOptions in matchingHandlers)
            {
                var result = await handlerOptions.Handler(appsyncEvent, context);

                // If handler returns false, deny subscription
                if (!result)
                {
                    return new AppSyncResolverEventsResponse { Authorized = false };
                }
            }

            return new AppSyncResolverEventsResponse { Authorized = true };
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error in subscribe handler: {ex.Message}");
            return new AppSyncResolverEventsResponse { Authorized = false };
        }
    }


// Helper to process a single event and handle exceptions
    private async Task<object> ProcessSingleEvent(Dictionary<string, object> payload,
        Func<Dictionary<string, object>, Task<object>> handler)
    {
        try
        {
            return await handler(payload);
        }
        catch (Exception ex)
        {
            return new Dictionary<string, object> { ["error"] = ex.Message };
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
            Id = id,
            Error = $"{ex.GetType().Name} - {ex.Message}"
        };
    }

    private List<string> FindMatchingPaths(IEnumerable<string> registeredPaths, string channelPath)
    {
        return registeredPaths.Where(pattern => IsMatch(pattern, channelPath)).ToList();
    }

    private bool IsMatch(string pattern, string path)
    {
        if (pattern == path)
        {
            return true;
        }

        // Convert wildcards to regex patterns
        var regexPattern = "^" + Regex.Escape(pattern)
                                   .Replace("\\*\\*", ".*") // ** matches any segments
                                   .Replace("\\*", "[^/]*") // * matches anything within a segment
                               + "$";

        return Regex.IsMatch(path, regexPattern);
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