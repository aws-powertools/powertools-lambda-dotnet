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
    private readonly RouteHandlerRegistry<AppSyncEventsRequest, object> _publishRoutes;
    private readonly RouteHandlerRegistry<AppSyncEventsRequest, bool> _subscribeRoutes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppSyncEventsResolver"/> class.
    /// </summary>
    public AppSyncEventsResolver()
    {
        _publishRoutes = new RouteHandlerRegistry<AppSyncEventsRequest, object>();
        _subscribeRoutes = new RouteHandlerRegistry<AppSyncEventsRequest, bool>();
    }

    #region OnPublish Methods
    

    /// <summary>
    /// Registers a sync handler for publish events on a specific channel path.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Sync handler without context</param>
    public AppSyncEventsResolver OnPublish(string path, Func<Dictionary<string, object>, object> handler)
    {
        RegisterPublishHandler(path, handler, false);
        return this;
    }

    /// <summary>
    /// Registers a sync handler with Lambda context for publish events on a specific channel path.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Sync handler with context</param>
    public AppSyncEventsResolver OnPublish(string path, Func<Dictionary<string, object>, ILambdaContext, object> handler)
    {
        RegisterPublishHandler(path, handler, false);
        return this;
    }

    #endregion

    #region OnPublishAsync Methods

    /// <summary>
    /// Explicitly registers an async handler for publish events on a specific channel path.
    /// Use this method when you want to clearly indicate that your handler is asynchronous.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Async handler without context</param>
    public AppSyncEventsResolver OnPublishAsync(string path, Func<Dictionary<string, object>, Task<object>> handler)
    {
        RegisterPublishHandler(path, handler, false);
        return this;
    }

    /// <summary>
    /// Explicitly registers an async handler with Lambda context for publish events on a specific channel path.
    /// Use this method when you want to clearly indicate that your handler is asynchronous.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Async handler with context</param>
    public AppSyncEventsResolver OnPublishAsync(string path, Func<Dictionary<string, object>, ILambdaContext, Task<object>> handler)
    {
        RegisterPublishHandler(path, handler, false);
        return this;
    }

    #endregion

    #region OnPublishAggregate Methods
    
    /// <summary>
    /// Registers a sync aggregate handler for publish events on a specific channel path.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Sync aggregate handler without context</param>
    public AppSyncEventsResolver OnPublishAggregate(string path, Func<AppSyncEventsRequest, AppSyncEventsResponse> handler)
    {
        RegisterAggregateHandler(path, handler);
        return this;
    }

    /// <summary>
    /// Registers a sync aggregate handler with Lambda context for publish events on a specific channel path.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Sync aggregate handler with context</param>
    public AppSyncEventsResolver OnPublishAggregate(string path, Func<AppSyncEventsRequest, ILambdaContext, AppSyncEventsResponse> handler)
    {
        RegisterAggregateHandler(path, handler);
        return this;
    }

    #endregion

    #region OnPublishAggregateAsync Methods

    /// <summary>
    /// Explicitly registers an async aggregate handler for publish events on a specific channel path.
    /// Use this method when you want to clearly indicate that your handler is asynchronous.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Async aggregate handler without context</param>
    public AppSyncEventsResolver OnPublishAggregateAsync(string path, Func<AppSyncEventsRequest, Task<AppSyncEventsResponse>> handler)
    {
        RegisterAggregateHandler(path, handler);
        return this;
    }

    /// <summary>
    /// Explicitly registers an async aggregate handler with Lambda context for publish events on a specific channel path.
    /// Use this method when you want to clearly indicate that your handler is asynchronous.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Async aggregate handler with context</param>
    public AppSyncEventsResolver OnPublishAggregateAsync(string path, Func<AppSyncEventsRequest, ILambdaContext, Task<AppSyncEventsResponse>> handler)
    {
        RegisterAggregateHandler(path, handler);
        return this;
    }

    #endregion

    #region OnSubscribe Methods

    /// <summary>
    /// Registers a sync handler for subscription events on a specific channel path.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Sync subscription handler without context</param>
    public AppSyncEventsResolver OnSubscribe(string path, Func<AppSyncEventsRequest, bool> handler)
    {
        RegisterSubscribeHandler(path, handler);
        return this;
    }

    /// <summary>
    /// Registers a sync handler with Lambda context for subscription events on a specific channel path.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Sync subscription handler with context</param>
    public AppSyncEventsResolver OnSubscribe(string path, Func<AppSyncEventsRequest, ILambdaContext, bool> handler)
    {
        RegisterSubscribeHandler(path, handler);
        return this;
    }

    #endregion

    #region OnSubscribeAsync Methods

    /// <summary>
    /// Explicitly registers an async handler for subscription events on a specific channel path.
    /// Use this method when you want to clearly indicate that your handler is asynchronous.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Async subscription handler without context</param>
    public AppSyncEventsResolver OnSubscribeAsync(string path, Func<AppSyncEventsRequest, Task<bool>> handler)
    {
        RegisterSubscribeHandler(path, handler);
        return this;
    }

    /// <summary>
    /// Explicitly registers an async handler with Lambda context for subscription events on a specific channel path.
    /// Use this method when you want to clearly indicate that your handler is asynchronous.
    /// </summary>
    /// <param name="path">The channel path to handle</param>
    /// <param name="handler">Async subscription handler with context</param>
    public AppSyncEventsResolver OnSubscribeAsync(string path, Func<AppSyncEventsRequest, ILambdaContext, Task<bool>> handler)
    {
        RegisterSubscribeHandler(path, handler);
        return this;
    }

    #endregion

    #region Handler Registration Methods

    private void RegisterPublishHandler(string path, Func<Dictionary<string, object>, Task<object>> handler, bool aggregate)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, object>
        {
            Path = path,
            Handler = async (evt, _) =>
            {
                var payload = evt.Events?.FirstOrDefault()?.Payload;
                return await handler(payload ?? new Dictionary<string, object>());
            },
            Aggregate = aggregate
        });
    }

    private void RegisterPublishHandler(string path, Func<Dictionary<string, object>, ILambdaContext, Task<object>> handler, bool aggregate)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, object>
        {
            Path = path,
            Handler = async (evt, ctx) =>
            {
                var payload = evt.Events?.FirstOrDefault()?.Payload;
                return await handler(payload ?? new Dictionary<string, object>(), ctx);
            },
            Aggregate = aggregate
        });
    }

    private void RegisterPublishHandler(string path, Func<Dictionary<string, object>, object> handler, bool aggregate)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, object>
        {
            Path = path,
            Handler = (evt, _) =>
            {
                var payload = evt.Events?.FirstOrDefault()?.Payload;
                return Task.FromResult(handler(payload ?? new Dictionary<string, object>()));
            },
            Aggregate = aggregate
        });
    }

    private void RegisterPublishHandler(string path, Func<Dictionary<string, object>, ILambdaContext, object> handler, bool aggregate)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, object>
        {
            Path = path,
            Handler = (evt, ctx) =>
            {
                var payload = evt.Events?.FirstOrDefault()?.Payload;
                return Task.FromResult(handler(payload ?? new Dictionary<string, object>(), ctx));
            },
            Aggregate = aggregate
        });
    }

    private void RegisterAggregateHandler(string path, Func<AppSyncEventsRequest, Task<AppSyncEventsResponse>> handler)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, object>
        {
            Path = path,
            Handler = async (evt, _) => await handler(evt),
            Aggregate = true
        });
    }

    private void RegisterAggregateHandler(string path, Func<AppSyncEventsRequest, ILambdaContext, Task<AppSyncEventsResponse>> handler)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, object>
        {
            Path = path,
            Handler = async (evt, ctx) => await handler(evt, ctx),
            Aggregate = true
        });
    }

    private void RegisterAggregateHandler(string path, Func<AppSyncEventsRequest, AppSyncEventsResponse> handler)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, object>
        {
            Path = path,
            Handler = (evt, _) => Task.FromResult((object)handler(evt)),
            Aggregate = true
        });
    }

    private void RegisterAggregateHandler(string path, Func<AppSyncEventsRequest, ILambdaContext, AppSyncEventsResponse> handler)
    {
        _publishRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, object>
        {
            Path = path,
            Handler = (evt, ctx) => Task.FromResult((object)handler(evt, ctx)),
            Aggregate = true
        });
    }

    private void RegisterSubscribeHandler(string path, Func<AppSyncEventsRequest, Task<bool>> handler)
    {
        _subscribeRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, bool>
        {
            Path = path,
            Handler = async (evt, _) => await handler(evt)
        });
    }

    private void RegisterSubscribeHandler(string path, Func<AppSyncEventsRequest, ILambdaContext, Task<bool>> handler)
    {
        _subscribeRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, bool>
        {
            Path = path,
            Handler = async (evt, ctx) => await handler(evt, ctx)
        });
    }

    private void RegisterSubscribeHandler(string path, Func<AppSyncEventsRequest, bool> handler)
    {
        _subscribeRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, bool>
        {
            Path = path,
            Handler = (evt, _) => Task.FromResult(handler(evt))
        });
    }

    private void RegisterSubscribeHandler(string path, Func<AppSyncEventsRequest, ILambdaContext, bool> handler)
    {
        _subscribeRoutes.Register(new RouteHandlerOptions<AppSyncEventsRequest, bool>
        {
            Path = path,
            Handler = (evt, ctx) => Task.FromResult(handler(evt, ctx))
        });
    }

    #endregion

    /// <summary>
    /// Resolves and processes an AppSync event through the registered handlers.
    /// </summary>
    /// <param name="appsyncEvent">The AppSync event to process</param>
    /// <param name="context">Lambda execution context</param>
    /// <returns>Response containing processed events or error information</returns>
    public AppSyncEventsResponse Resolve(AppSyncEventsRequest appsyncEvent, ILambdaContext context)
    {
        return ResolveAsync(appsyncEvent, context).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Resolves and processes an AppSync event through the registered handlers.
    /// </summary>
    /// <param name="appsyncEvent">The AppSync event to process</param>
    /// <param name="context">Lambda execution context</param>
    /// <returns>Response containing processed events or error information</returns>
    public async Task<AppSyncEventsResponse> ResolveAsync(AppSyncEventsRequest appsyncEvent, ILambdaContext context)
    {
        if (IsPublishEvent(appsyncEvent))
        {
            return await HandlePublishEvent(appsyncEvent, context);
        }

        if (IsSubscribeEvent(appsyncEvent))
        {
            return (await HandleSubscribeEvent(appsyncEvent, context))!;
        }

        throw new InvalidOperationException("Unknown event type");
    }

    private async Task<AppSyncEventsResponse> HandlePublishEvent(AppSyncEventsRequest appsyncEvent,
        ILambdaContext context)
    {
        var channelPath = appsyncEvent.Info?.Channel?.Path;
        var handlerOptions = _publishRoutes.ResolveFirst(channelPath);

        context.Logger.LogInformation($"Resolving publish event for path: {channelPath}");

        if (handlerOptions == null)
        {
            // Return unchanged events if no handler found
            var events = appsyncEvent.Events?
                .Select(e => new AppSyncEvent
                {
                    Id = e.Id,
                    Payload = e.Payload
                })
                .ToList();
            return new AppSyncEventsResponse { Events = events };
        }

        var results = new List<AppSyncEvent>();

        if (handlerOptions.Aggregate)
        {
            try
            {
                // Process entire event in one call
                var handlerResult = await handlerOptions.Handler(appsyncEvent, context);
                if (handlerResult is AppSyncEventsResponse { Events: not null } result)
                {
                    return result;
                }

                // Handle unexpected return type
                return new AppSyncEventsResponse
                {
                    Error = "Handler returned an invalid response type"
                };
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new AppSyncEventsResponse
                {
                    Error = $"{ex.GetType().Name} - {ex.Message}"
                };
            }
        }
        else
        {
            // Process each event individually
            if (appsyncEvent.Events == null) return new AppSyncEventsResponse { Events = results };
            foreach (var eventItem in appsyncEvent.Events)
            {
                try
                {
                    var result = await handlerOptions.Handler(
                        new AppSyncEventsRequest
                        {
                            Info = appsyncEvent.Info,
                            Events = [eventItem]
                        }, context);

                    var payload = ConvertToPayload(result, out var error);
                    if (error != null)
                    {
                        results.Add(new AppSyncEvent
                        {
                            Id = eventItem.Id,
                            Error = error
                        });
                    }
                    else
                    {
                        results.Add(new AppSyncEvent
                        {
                            Id = eventItem.Id,
                            Payload = payload
                        });
                    }
                }
                catch (UnauthorizedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    results.Add(FormatErrorResponse(ex, eventItem.Id!));
                }
            }
        }

        return new AppSyncEventsResponse { Events = results };
    }

    /// <summary>
    /// Handles subscription events.
    /// Returns null on success, error response on failure.
    /// </summary>
    private async Task<AppSyncEventsResponse?> HandleSubscribeEvent(AppSyncEventsRequest appsyncEvent,
        ILambdaContext context)
    {
        var channelPath = appsyncEvent.Info?.Channel?.Path;
        var channelBase = $"/{appsyncEvent.Info?.Channel?.Segments?[0]}";

        // Find matching subscribe handler
        var subscribeHandler = _subscribeRoutes.ResolveFirst(channelPath);
        if (subscribeHandler == null)
        {
            return null;
        }

        // Check if there's ANY publish handler for the base channel namespace
        bool hasAnyPublishHandler = _publishRoutes.GetAllHandlers()
            .Any(h => h.Path.StartsWith(channelBase));

        if (!hasAnyPublishHandler)
        {
            return null;
        }

        try
        {
            var result = await subscribeHandler.Handler(appsyncEvent, context);
            return !result ? new AppSyncEventsResponse { Error = "Subscription failed" } : null;
        }
        catch (UnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error in subscribe handler: {ex.Message}");
            return new AppSyncEventsResponse { Error = ex.Message };
        }
    }

    private Dictionary<string, object>? ConvertToPayload(object result, out string? error)
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

    private AppSyncEvent FormatErrorResponse(Exception ex, string id)
    {
        return new AppSyncEvent
        {
            Id = id,
            Error = $"{ex.GetType().Name} - {ex.Message}"
        };
    }

    private bool IsPublishEvent(AppSyncEventsRequest appsyncEvent)
    {
        return appsyncEvent.Info?.Operation == AppSyncEventsOperation.Publish;
    }

    private bool IsSubscribeEvent(AppSyncEventsRequest appsyncEvent)
    {
        return appsyncEvent.Info?.Operation == AppSyncEventsOperation.Subscribe;
    }
}

/// <summary>
/// Exception thrown when subscription validation fails.
/// This exception causes the Lambda invocation to fail, returning an error to AppSync.
/// </summary>
public class UnauthorizedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    public UnauthorizedException(string message) : base(message)
    {
    }
}