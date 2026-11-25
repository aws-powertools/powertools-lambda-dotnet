using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Class Metrics.
///     Implements the <see cref="IMetrics" />
/// </summary>
/// <seealso cref="IMetrics" />
public class Metrics : IMetrics, IDisposable
{
    /// <summary>
    /// Static lock object for thread-safe instance creation
    /// </summary>
    private static readonly object _instanceLock = new();
    
    /// <summary>
    ///    Gets or sets the instance.
    /// </summary>
    public static IMetrics Instance
    {
        get
        {
            if (_instance != null)
                return _instance;
                
            lock (_instanceLock)
            {
                // Double-check after acquiring lock
                return _instance ??= new Metrics(PowertoolsConfigurations.Instance, consoleWrapper: new ConsoleWrapper());
            }
        }
        private set => _instance = value;
    }
    
    /// <summary>
    /// Gets DefaultDimensions
    /// </summary>
    public static Dictionary<string, string> DefaultDimensions => Instance.Options.DefaultDimensions;
    
    /// <summary>
    /// Gets Namespace
    /// </summary>
    public static string Namespace => Instance.Options.Namespace;
    
    /// <summary>
    /// Gets Service 
    /// </summary>
    public static string Service => Instance.Options.Service;

    /// <inheritdoc />
    public MetricsOptions Options => _options ??
        new()
        {
            CaptureColdStart = _captureColdStartEnabled,
            Namespace = GetNamespace(),
            Service = GetService(),
            RaiseOnEmptyMetrics = _raiseOnEmptyMetrics,
            DefaultDimensions = GetDefaultDimensions(),
            FunctionName = _functionName
        };

    /// <summary>
    ///     The instance
    /// </summary>
    private static volatile IMetrics _instance;

    /// <summary>
    ///     A MetricsContext bound to an async flow. Scopes form a chain so that a nested
    ///     <c>[Metrics]</c> method gets its own context and its caller's context is used again once
    ///     the nested one has ended.
    /// </summary>
    private sealed class ContextScope
    {
        internal ContextScope(MetricsContext context, ContextScope parent, bool isInvocation)
        {
            Context = context;
            Parent = parent;
            IsInvocation = isInvocation;
            Depth = parent == null ? 0 : parent.Depth + 1;
        }

        internal MetricsContext Context { get; }
        internal ContextScope Parent { get; }
        internal bool IsInvocation { get; }
        internal int Depth { get; }
    }

    /// <summary>
    ///     The context of the current invocation. AsyncLocal flows with the ExecutionContext, so the
    ///     same context is used before and after an <c>await</c> (whatever thread the continuation
    ///     runs on) and by child tasks started from the invocation (<c>Task.Run</c>,
    ///     <c>Task.WhenAll</c>, <c>Parallel.ForEachAsync</c>).
    ///     Concurrent invocations (AWS_LAMBDA_MAX_CONCURRENCY &gt; 1) run in separate flows and
    ///     therefore never share a context.
    /// </summary>
    private static readonly AsyncLocal<ContextScope> _currentScope = new();

    /// <summary>
    ///     Registry of live contexts. A context is live while it is registered here; once removed
    ///     (when its invocation ends, or on reset) every flow that still references it ignores it.
    ///     Keys are held weakly, so contexts created lazily by code that never ends them (manual
    ///     usage without the <c>[Metrics]</c> attribute) are dropped once their flow is gone.
    ///     Used to apply default dimension changes to all live contexts.
    /// </summary>
    private static readonly ConditionalWeakTable<MetricsContext, object> _liveContexts = new();

    /// <summary>
    ///     Value stored for every registered context.
    /// </summary>
    private static readonly object _liveMarker = new();

    /// <summary>
    ///     Upper bound for the scope chain. Only reached if invocations keep failing (so they never
    ///     end their scope) while their caller's flow keeps a reference; the chain is cut there so
    ///     it can't grow without bound.
    /// </summary>
    private const int MaxScopeDepth = 16;

    /// <summary>
    ///     Gets the MetricsContext for the current invocation.
    ///     Creates one in the current async flow if there is none, for code that doesn't use the
    ///     <c>[Metrics]</c> attribute (manual usage, ASP.NET Core).
    /// </summary>
    private static MetricsContext CurrentContext
    {
        get
        {
            var existing = ExistingContext;
            if (existing != null)
                return existing;

            var ctx = CreateContext();
            _currentScope.Value = new ContextScope(ctx, null, false);
            return ctx;
        }
    }

    /// <summary>
    ///     Gets the live MetricsContext for the current invocation without creating one.
    /// </summary>
    private static MetricsContext ExistingContext => FindLiveScope(_currentScope.Value, false)?.Context;

    private static bool IsLive(MetricsContext context) => _liveContexts.TryGetValue(context, out _);

    private static ContextScope FindLiveScope(ContextScope scope, bool invocationOnly)
    {
        while (scope != null && (!IsLive(scope.Context) || (invocationOnly && !scope.IsInvocation)))
            scope = scope.Parent;
        return scope;
    }

    /// <summary>
    ///     Creates and registers a context initialized from the shared configuration.
    /// </summary>
    private static MetricsContext CreateContext()
    {
        var ctx = new MetricsContext();
        // Copy shared configuration to new context
        var ns = _sharedNamespace;
        if (!string.IsNullOrWhiteSpace(ns))
            ctx.SetNamespace(ns);

        var svc = _sharedService;
        if (!string.IsNullOrWhiteSpace(svc))
        {
            ctx.SetService(svc);
        }

        // Copy default dimensions (including Service dimension if set)
        lock (_defaultDimensionsLock)
        {
            if (_sharedDefaultDimensions.Count > 0)
            {
                ctx.SetDefaultDimensions(new List<DimensionSet>(_sharedDefaultDimensions));
            }
            else if (!string.IsNullOrWhiteSpace(svc))
            {
                // If no shared default dimensions but service is set, add Service dimension
                ctx.SetDefaultDimensions(new List<DimensionSet>(new[] { new DimensionSet("Service", svc) }));
            }

            // Register under the same lock that default dimension updates hold, so an update
            // either sees this context or happened before it copied the shared dimensions
            _liveContexts.AddOrUpdate(ctx, _liveMarker);
        }

        return ctx;
    }

    /// <summary>
    ///     Snapshot of all live contexts.
    /// </summary>
    private static List<MetricsContext> LiveContexts()
    {
        var contexts = new List<MetricsContext>();
        foreach (var kvp in (IEnumerable<KeyValuePair<MetricsContext, object>>)_liveContexts)
            contexts.Add(kvp.Key);
        return contexts;
    }

    /// <summary>
    ///     Starts a new context for an invocation in the current async flow. Called when a
    ///     <c>[Metrics]</c> method starts; everything the invocation runs, including child tasks,
    ///     inherits it.
    /// </summary>
    internal static void BeginInvocationContext()
    {
        var parent = FindLiveScope(_currentScope.Value, false);
        if (parent != null && parent.Depth >= MaxScopeDepth)
            parent = null;

        _currentScope.Value = new ContextScope(CreateContext(), parent, true);
    }

    /// <summary>
    ///     Ends the current invocation's context. Called when a <c>[Metrics]</c> method ends, after
    ///     its metrics are flushed. The context is unregistered, so nothing (another invocation that
    ///     runs on the same thread, or a task that outlives the invocation) can write to it or
    ///     flush it again.
    /// </summary>
    internal static void EndInvocationContext()
    {
        var scope = FindLiveScope(_currentScope.Value, true);
        if (scope == null)
            return;

        _liveContexts.Remove(scope.Context);
        _currentScope.Value = scope.Parent;
    }

    /// <summary>
    ///     Shared namespace across all threads (configuration-level)
    /// </summary>
    private static string _sharedNamespace;

    /// <summary>
    ///     Shared service name across all threads (configuration-level)
    /// </summary>
    private static string _sharedService;

    /// <summary>
    ///     Shared default dimensions across all threads (by design per requirements)
    /// </summary>
    private static readonly List<DimensionSet> _sharedDefaultDimensions = new();

    /// <summary>
    ///     Lock for shared default dimensions
    /// </summary>
    private static readonly object _defaultDimensionsLock = new();

    /// <summary>
    ///     The Powertools for AWS Lambda (.NET) configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     If true, Powertools for AWS Lambda (.NET) will throw an exception on empty metrics when trying to flush
    /// </summary>
    private bool _raiseOnEmptyMetrics;

    /// <summary>
    ///     The capture cold start enabled
    /// </summary>
    private bool _captureColdStartEnabled;

    /// <summary>
    /// Shared synchronization object
    /// </summary>
    private readonly object _lockObj = new();
    
    /// <summary>
    /// Function name is used for metric dimension across all metrics.
    /// </summary>
    private string _functionName;

    /// <summary>
    ///   The options
    /// </summary>
    private readonly MetricsOptions _options;

    /// <summary>
    ///    The console wrapper for console output
    /// </summary>
    private readonly IConsoleWrapper _consoleWrapper;

    /// <summary>
    ///   Gets a value indicating whether metrics are disabled.
    /// </summary>
    private bool _disabled;

    /// <summary>
    ///    Initializes a new instance of the <see cref="Metrics" /> class.
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IMetrics Configure(Action<MetricsOptions> configure)
    {
        var options = new MetricsOptions();
        configure(options);

        if (!string.IsNullOrEmpty(options.Namespace))
            SetNamespace(options.Namespace);

        if (options.RaiseOnEmptyMetrics.HasValue)
            Instance.SetRaiseOnEmptyMetrics(options.RaiseOnEmptyMetrics.Value);
        if (options.CaptureColdStart.HasValue)
            Instance.SetCaptureColdStart(options.CaptureColdStart.Value);

        // Set default dimensions before service so that SetService can add Service to the dimensions
        if (options.DefaultDimensions != null)
            SetDefaultDimensions(options.DefaultDimensions);

        // Set service after default dimensions so Service dimension is preserved
        if (!string.IsNullOrEmpty(options.Service))
            Instance.SetService(options.Service);

        if (!string.IsNullOrEmpty(options.FunctionName))
            Instance.SetFunctionName(options.FunctionName);
        
        return Instance;
    }

    /// <summary>
    ///    Sets the function name.
    /// </summary>
    /// <param name="functionName"></param>
    void IMetrics.SetFunctionName(string functionName)
    {
        _functionName = functionName;
    }

    /// <summary>
    ///     Creates a Metrics object that provides features to send metrics to Amazon Cloudwatch using the Embedded metric
    ///     format (EMF). See
    ///     https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html
    /// </summary>
    /// <param name="powertoolsConfigurations">Powertools for AWS Lambda (.NET) Configuration</param>
    /// <param name="nameSpace">Metrics Namespace Identifier</param>
    /// <param name="service">Metrics Service Name</param>
    /// <param name="raiseOnEmptyMetrics">Instructs metrics validation to throw exception if no metrics are provided</param>
    /// <param name="captureColdStartEnabled">Instructs metrics capturing the ColdStart is enabled</param>
    /// <param name="consoleWrapper">For console output</param>
    /// <param name="options">MetricsOptions</param>
    internal Metrics(IPowertoolsConfigurations powertoolsConfigurations, string nameSpace = null, string service = null,
        bool raiseOnEmptyMetrics = false, bool captureColdStartEnabled = false, IConsoleWrapper consoleWrapper = null, MetricsOptions options = null)
    {
        _powertoolsConfigurations = powertoolsConfigurations;
        _consoleWrapper = consoleWrapper;
        _raiseOnEmptyMetrics = raiseOnEmptyMetrics;
        _captureColdStartEnabled = captureColdStartEnabled;
        _options = options;

        _disabled = _powertoolsConfigurations.MetricsDisabled;
        
        Instance = this;

        // set namespace and service always
        SetNamespace(nameSpace);
        SetService(service);
    }

    /// <inheritdoc />
    void IMetrics.AddMetric(string key, double value, MetricUnit unit, MetricResolution resolution)
    {
        if(_disabled)
            return;
        
        if (Instance != null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(
                    nameof(key),
                    "'AddMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.");
            if (key.Length > 255)
                throw new ArgumentOutOfRangeException(
                    nameof(key),
                    "'AddMetric' method requires a valid metrics key. Key exceeds the allowed length constraint.");

            if (value < 0)
            {
                throw new ArgumentException(
                    "'AddMetric' method requires a valid metrics value. Value must be >= 0.", nameof(value));
            }

            var context = CurrentContext;

            // Child tasks of an invocation share its context: the overflow check, the overflow
            // flush and the add must not interleave with another task's add or flush
            lock (context.SyncRoot)
            {
                var metrics = context.GetMetrics();

                if (metrics.Count > 0 &&
                    (metrics.Count == PowertoolsConfigurations.MaxMetrics ||
                     GetExistingMetric(metrics, key)?.Values.Count == PowertoolsConfigurations.MaxMetrics))
                {
                    FlushContext(context, true);
                }

                context.AddMetric(key, value, unit, resolution);
            }
        }
        else
        {
            _consoleWrapper.Debug(
                $"##WARNING##: Metrics should be initialized in Handler method before calling {nameof(AddMetric)} method.");
        }
    }

    /// <inheritdoc />
    void IMetrics.SetNamespace(string nameSpace)
    {
        var ns = !string.IsNullOrWhiteSpace(nameSpace)
            ? nameSpace
            : GetNamespace() ?? _powertoolsConfigurations.MetricsNamespace;
        
        // Store in shared state for new contexts
        _sharedNamespace = ns;

        // Update the current invocation's context, if there is one. Configuration doesn't create a
        // context: one created at startup would be inherited by every invocation.
        ExistingContext?.SetNamespace(ns);
    }


    /// <summary>
    ///     Implements interface to get service name
    /// </summary>
    /// <returns>System.String.</returns>
    private string GetService()
    {
        try
        {
            var context = ExistingContext;
            return context != null ? context.GetService() : _sharedService;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    void IMetrics.AddDimension(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key),
                "'AddDimension' method requires a valid dimension key. 'Null' or empty values are not allowed.");

        var context = CurrentContext;
        lock (context.SyncRoot)
        {
            context.AddDimension(key, value);
        }
    }

    /// <inheritdoc />
    void IMetrics.AddMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key),
                "'AddMetadata' method requires a valid metadata key. 'Null' or empty values are not allowed.");

        var context = CurrentContext;
        lock (context.SyncRoot)
        {
            context.AddMetadata(key, value);
        }
    }

    /// <inheritdoc />
    void IMetrics.SetDefaultDimensions(Dictionary<string, string> defaultDimensions)
    {
        foreach (var item in defaultDimensions)
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                throw new ArgumentNullException(nameof(item.Key),
                    "'SetDefaultDimensions' method requires a valid key pair. 'Null' or empty values are not allowed.");

        var dimensionsList = DictionaryToList(defaultDimensions);
        
        // Update shared default dimensions (shared across all invocations by design)
        lock (_defaultDimensionsLock)
        {
            _sharedDefaultDimensions.Clear();
            _sharedDefaultDimensions.AddRange(dimensionsList);

            // Preserve Service dimension if it was set
            if (!string.IsNullOrWhiteSpace(_sharedService) &&
                !_sharedDefaultDimensions.Any(d => d.Dimensions.ContainsKey("Service")))
            {
                _sharedDefaultDimensions.Add(new DimensionSet("Service", _sharedService));
            }

            // Update all live contexts, including the current invocation's
            foreach (var context in LiveContexts())
            {
                context.SetDefaultDimensions(new List<DimensionSet>(_sharedDefaultDimensions));
            }
        }
    }

    /// <inheritdoc />
    void IMetrics.Flush(bool metricsOverflow)
    {
        if(_disabled)
            return;

        // Flushing doesn't create a context: with none there is nothing to publish
        FlushContext(ExistingContext ?? new MetricsContext(), metricsOverflow);
    }

    /// <summary>
    ///     Flushes a specific context's metrics.
    /// </summary>
    /// <param name="context">The context to flush</param>
    /// <param name="metricsOverflow">If true, indicates overflow flush (don't clear dimensions)</param>
    private void FlushContext(MetricsContext context, bool metricsOverflow)
    {
        // Serialize and clear as one step, so a metric added concurrently by a child task is
        // either in this payload or kept for the next one, never dropped
        lock (context.SyncRoot)
        {
            if (context.GetMetrics().Count == 0
                && _raiseOnEmptyMetrics)
                throw new SchemaValidationException(true);

            if (context.IsSerializable)
            {
                var emfPayload = context.Serialize();

                _consoleWrapper.WriteLine(emfPayload);

                context.ClearMetrics();

                if (!metricsOverflow) context.ClearNonDefaultDimensions();
            }
            else
            {
                if (!_captureColdStartEnabled)
                    _consoleWrapper.WriteLine(
                        "##User-WARNING## No application metrics to publish. The cold-start metric may be published if enabled. If application metrics should never be empty, consider using 'RaiseOnEmptyMetrics = true'");
            }
        }
    }

    /// <inheritdoc />
    void IMetrics.ClearDefaultDimensions()
    {
        lock (_defaultDimensionsLock)
        {
            // Clear shared default dimensions
            _sharedDefaultDimensions.Clear();

            // Clear in all live contexts
            foreach (var context in LiveContexts())
            {
                context.ClearDefaultDimensions();
            }
        }
    }

    /// <inheritdoc />
    void IMetrics.SetService(string service)
    {
        // this needs to check if service is set through code or env variables
        // the default value service_undefined has to be ignored and return null so it is not added as default   
        var parsedService = !string.IsNullOrWhiteSpace(service)
            ? service
            : _powertoolsConfigurations.Service == "service_undefined"
                ? null
                : _powertoolsConfigurations.Service;

        if (parsedService != null)
        {
            // Store in shared state for new contexts
            _sharedService = parsedService;

            // Add Service to shared default dimensions
            lock (_defaultDimensionsLock)
            {
                // Remove existing Service dimension if present
                _sharedDefaultDimensions.RemoveAll(d => d.Dimensions.ContainsKey("Service"));
                // Add new Service dimension
                _sharedDefaultDimensions.Add(new DimensionSet("Service", parsedService));
            }

            // Update the current invocation's context, if there is one (configuration doesn't
            // create a context)
            var context = ExistingContext;
            if (context != null)
            {
                context.SetService(parsedService);

                // Update default dimensions in current context with the shared list
                lock (_defaultDimensionsLock)
                {
                    context.SetDefaultDimensions(new List<DimensionSet>(_sharedDefaultDimensions));
                }
            }
        }
    }

    /// <inheritdoc />
    public void SetRaiseOnEmptyMetrics(bool raiseOnEmptyMetrics)
    {
        _raiseOnEmptyMetrics = raiseOnEmptyMetrics;
    }

    /// <inheritdoc />
    public void SetCaptureColdStart(bool captureColdStart)
    {
        _captureColdStartEnabled = captureColdStart;
    }

    private Dictionary<string, string> GetDefaultDimensions()
    {
        // Read from shared state to ensure consistency across threads
        lock (_defaultDimensionsLock)
        {
            return ListToDictionary(new List<DimensionSet>(_sharedDefaultDimensions));
        }
    }

    /// <inheritdoc />
    void IMetrics.PushSingleMetric(string name, double value, MetricUnit unit, string nameSpace,
        string service, Dictionary<string, string> dimensions, MetricResolution resolution)
    {
        if(_disabled)
            return;
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name),
                "'PushSingleMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.");

        var context = new MetricsContext();
        context.SetNamespace(nameSpace ?? GetNamespace());
        
        var parsedService = !string.IsNullOrWhiteSpace(service)
            ? service
            : _powertoolsConfigurations.Service == "service_undefined"
                ? null
                : _powertoolsConfigurations.Service;
        
        if (!string.IsNullOrWhiteSpace(parsedService))
        {
            context.SetService(parsedService);
            context.AddDimension("Service", parsedService);
        }

        if (dimensions != null)
        {
            var dimensionsList = DictionaryToList(dimensions);
            context.AddDimensions(dimensionsList);
        }

        context.AddMetric(name, value, unit, resolution);

        Flush(context);
    }


    /// <summary>
    ///     Implementation of IDisposable interface
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        // Cleanup
        if (disposing)
        {
            Instance.Flush();
        }
    }

    /// <summary>
    ///     Adds new metric to memory.
    /// </summary>
    /// <param name="key">Metric Key. Must not be null, empty or whitespace</param>
    /// <param name="value">Metric Value</param>
    /// <param name="unit">Metric Unit</param>
    /// <param name="resolution"></param>
    public static void AddMetric(string key, double value, MetricUnit unit = MetricUnit.None,
        MetricResolution resolution = MetricResolution.Default)
    {
        Instance.AddMetric(key, value, unit, resolution);
    }

    /// <summary>
    ///     Sets metrics namespace identifier.
    /// </summary>
    /// <param name="nameSpace">Metrics Namespace Identifier</param>
    public static void SetNamespace(string nameSpace)
    {
        Instance.SetNamespace(nameSpace);
    }
    
    /// <summary>
    ///     Sets the service name for the metrics.
    /// </summary>
    /// <param name="service">The service name.</param>
    public static void SetService(string service)
    {
        Instance.SetService(service);
    }

    /// <summary>
    ///     Retrieves namespace identifier.
    /// </summary>
    /// <returns>Namespace identifier</returns>
    public string GetNamespace()
    {
        try
        {
            var context = ExistingContext;
            var ns = context != null ? context.GetNamespace() : _sharedNamespace;
            return ns ?? _powertoolsConfigurations.MetricsNamespace;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Adds new dimension to memory.
    /// </summary>
    /// <param name="key">Dimension key. Must not be null, empty or whitespace.</param>
    /// <param name="value">Dimension value</param>
    public static void AddDimension(string key, string value)
    {
        Instance.AddDimension(key, value);
    }

    /// <summary>
    ///     Adds metadata to memory.
    /// </summary>
    /// <param name="key">Metadata key. Must not be null, empty or whitespace</param>
    /// <param name="value">Metadata value</param>
    public static void AddMetadata(string key, object value)
    {
        Instance.AddMetadata(key, value);
    }

    /// <summary>
    ///     Set default dimension list
    /// </summary>
    /// <param name="defaultDimensions">Default Dimension List</param>
    public static void SetDefaultDimensions(Dictionary<string, string> defaultDimensions)
    {
        Instance.SetDefaultDimensions(defaultDimensions);
    }

    /// <summary>
    ///     Clears both default dimensions and dimensions lists
    /// </summary>
    public static void ClearDefaultDimensions()
    {
        Instance.ClearDefaultDimensions();
    }

    /// <summary>
    ///     Flushes metrics in Embedded Metric Format (EMF) to Standard Output. In Lambda, this output is collected
    ///     automatically and sent to Cloudwatch.
    /// </summary>
    /// <param name="context">If context is provided it is serialized instead of the global context object</param>
    private void Flush(MetricsContext context)
    {
        var emfPayload = context.Serialize();

        _consoleWrapper.WriteLine(emfPayload);
    }

    /// <summary>
    ///     Pushes single metric to CloudWatch using Embedded Metric Format. This can be used to push metrics with a different
    ///     context.
    /// </summary>
    /// <param name="name">Metric Name. Metric key cannot be null, empty or whitespace</param>
    /// <param name="value">Metric Value</param>
    /// <param name="unit">Metric Unit</param>
    /// <param name="nameSpace">Metric Namespace</param>
    /// <param name="service">Service Name</param>
    /// <param name="dimensions">Default dimensions list</param>
    /// <param name="resolution">Metrics resolution</param>
    public static void PushSingleMetric(string name, double value, MetricUnit unit, string nameSpace = null,
        string service = null, Dictionary<string, string> dimensions = null,
        MetricResolution resolution = MetricResolution.Default)
    {
        Instance.PushSingleMetric(name, value, unit, nameSpace, service, dimensions,
            resolution);
    }

    /// <summary>
    ///     Helper method to convert default dimensions dictionary to list
    /// </summary>
    /// <param name="defaultDimensions">Default dimensions dictionary</param>
    /// <returns>Default dimensions list</returns>
    private List<DimensionSet> DictionaryToList(Dictionary<string, string> defaultDimensions)
    {
        var dimensionsList = new List<DimensionSet>();
        if (defaultDimensions != null)
            foreach (var item in defaultDimensions)
                dimensionsList.Add(new DimensionSet(item.Key, item.Value));

        return dimensionsList;
    }

    private Dictionary<string, string> ListToDictionary(List<DimensionSet> dimensions)
    {
        var dictionary = new Dictionary<string, string>();
        if (dimensions == null)
            return dictionary;

        foreach (var dimensionSet in dimensions)
        {
            if (dimensionSet?.Dimensions == null)
                continue;
                
            foreach (var kvp in dimensionSet.Dimensions)
            {
                dictionary[kvp.Key] = kvp.Value;
            }
        }
        
        return dictionary;
    }
    
    /// <summary>
    ///     Captures the cold start metric.
    /// </summary>
    /// <param name="context">The ILambdaContext.</param>
    void IMetrics.CaptureColdStartMetric(ILambdaContext context)
    {
        if (Options.CaptureColdStart == null || !Options.CaptureColdStart.Value) return;
        
        // bring default dimensions if exist; copy so the configured options are never mutated
        var defaultDimensions = Options?.DefaultDimensions;
        var dimensions = defaultDimensions != null ? new Dictionary<string, string>(defaultDimensions) : null;

        var functionName = Options?.FunctionName ?? context?.FunctionName ?? "";
        if (!string.IsNullOrWhiteSpace(functionName))
        {
            dimensions ??= new Dictionary<string, string>();
            dimensions["FunctionName"] = functionName;
        }

        PushSingleMetric(
            "ColdStart",
            1.0,
            MetricUnit.Count,
            Options?.Namespace ?? "",
            Options?.Service ?? "",
            dimensions
        );
    }
    
    /// <inheritdoc />
    void IMetrics.AddDimensions(params (string key, string value)[] dimensions)
    {
        if (dimensions == null || dimensions.Length == 0)
            return;

        // Validate all dimensions first
        foreach (var (key, value) in dimensions)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(dimensions),
                    "'AddDimensions' method requires valid dimension keys. 'Null' or empty values are not allowed.");

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(dimensions),
                    "'AddDimensions' method requires valid dimension values. 'Null' or empty values are not allowed.");
        }

        // Create a new dimension set with all dimensions
        var dimensionSet = new DimensionSet(dimensions[0].key, dimensions[0].value);
    
        // Add remaining dimensions to the same set
        for (var i = 1; i < dimensions.Length; i++)
        {
            dimensionSet.Dimensions.TryAdd(dimensions[i].key, dimensions[i].value);
        }

        // Add the dimensionSet to the current invocation's context
        var context = CurrentContext;
        lock (context.SyncRoot)
        {
            context.AddDimensions([dimensionSet]);
        }
    }
    
    /// <summary>
    ///     Adds multiple dimensions at once.
    /// </summary>
    /// <param name="dimensions">Array of key-value tuples representing dimensions.</param>
    public static void AddDimensions(params (string key, string value)[] dimensions)
    {
        Instance.AddDimensions(dimensions);
    }
    
    /// <summary>
    ///     Flushes the metrics.
    /// </summary>
    /// <param name="metricsOverflow">If set to <c>true</c>, indicates a metrics overflow.</param>
    public static void Flush(bool metricsOverflow = false)
    {
        Instance.Flush(metricsOverflow);
    }

    /// <summary>
    ///     Searches for an existing metric by name
    /// </summary>
    /// <param name="metrics">The metrics collection to search</param>
    /// <param name="key">The metric name to search for</param>
    /// <returns>The found metric or null if not found</returns>
    private static MetricDefinition GetExistingMetric(List<MetricDefinition> metrics, string key)
    {
        if (metrics == null || string.IsNullOrEmpty(key))
            return null;
            
        foreach (var metric in metrics)
        {
            if (metric != null && string.Equals(metric.Name, key, StringComparison.Ordinal))
            {
                return metric;
            }
        }
        return null;
    }

    /// <summary>
    ///     Helper method for testing purposes. Clears static instance between test execution
    /// </summary>
    internal static void ResetForTest()
    {
        Instance = null;
        // Unregistering every context also kills the ones other flows still reference
        _liveContexts.Clear();
        _currentScope.Value = null;
        _sharedNamespace = null;
        _sharedService = null;
        lock (_defaultDimensionsLock)
        {
            _sharedDefaultDimensions.Clear();
        }
    }

    /// <summary>
    ///     Discards the current async flow's context without flushing it. The next metric added in
    ///     this flow starts a new context.
    /// </summary>
    internal static void ClearCurrentContext()
    {
        var context = ExistingContext;
        if (context != null)
            _liveContexts.Remove(context);
        _currentScope.Value = null;
    }

    /// <summary>
    /// For testing purposes, resets the Instance to the provided metrics instance.
    /// </summary>
    /// <param name="metricsInstance"></param>
    public static void UseMetricsForTests(IMetrics metricsInstance)
    {
        Instance = metricsInstance;
    }
}