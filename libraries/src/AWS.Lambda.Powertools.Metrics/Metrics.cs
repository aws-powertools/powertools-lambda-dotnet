using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    ///     Thread-safe dictionary for per-thread context storage.
    ///     Uses ManagedThreadId as key to ensure isolation when Lambda processes
    ///     multiple concurrent requests (AWS_LAMBDA_MAX_CONCURRENCY > 1).
    /// </summary>
    private static readonly ConcurrentDictionary<int, MetricsContext> _threadContexts = new();

    /// <summary>
    ///     Gets the MetricsContext for the current thread.
    ///     Creates a new context if one doesn't exist for this thread.
    /// </summary>
    private MetricsContext CurrentContext
    {
        get
        {
            var threadId = Environment.CurrentManagedThreadId;
            return _threadContexts.GetOrAdd(threadId, _ =>
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
                }
                return ctx;
            });
        }
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
            var metrics = context.GetMetrics();

            if (metrics.Count > 0 &&
                (metrics.Count == PowertoolsConfigurations.MaxMetrics ||
                 GetExistingMetric(metrics, key)?.Values.Count == PowertoolsConfigurations.MaxMetrics))
            {
                FlushContext(context, true);
            }

            context.AddMetric(key, value, unit, resolution);
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
        
        // Store in shared state for new thread contexts
        _sharedNamespace = ns;
        
        // Update current thread's context
        CurrentContext.SetNamespace(ns);
    }


    /// <summary>
    ///     Implements interface to get service name
    /// </summary>
    /// <returns>System.String.</returns>
    private string GetService()
    {
        try
        {
            return CurrentContext.GetService();
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

        CurrentContext.AddDimension(key, value);
    }

    /// <inheritdoc />
    void IMetrics.AddMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key),
                "'AddMetadata' method requires a valid metadata key. 'Null' or empty values are not allowed.");

        CurrentContext.AddMetadata(key, value);
    }

    /// <inheritdoc />
    void IMetrics.SetDefaultDimensions(Dictionary<string, string> defaultDimensions)
    {
        foreach (var item in defaultDimensions)
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                throw new ArgumentNullException(nameof(item.Key),
                    "'SetDefaultDimensions' method requires a valid key pair. 'Null' or empty values are not allowed.");

        var dimensionsList = DictionaryToList(defaultDimensions);
        
        // Update shared default dimensions (shared across all threads by design)
        lock (_defaultDimensionsLock)
        {
            _sharedDefaultDimensions.Clear();
            _sharedDefaultDimensions.AddRange(dimensionsList);
        }
        
        // Update all existing thread contexts
        foreach (var kvp in _threadContexts)
        {
            kvp.Value.SetDefaultDimensions(new List<DimensionSet>(dimensionsList));
        }
        
        // Also update current context (in case it was just created)
        CurrentContext.SetDefaultDimensions(new List<DimensionSet>(dimensionsList));
    }

    /// <inheritdoc />
    void IMetrics.Flush(bool metricsOverflow)
    {
        if(_disabled)
            return;

        FlushContext(CurrentContext, metricsOverflow);
    }

    /// <summary>
    ///     Flushes a specific context's metrics.
    /// </summary>
    /// <param name="context">The context to flush</param>
    /// <param name="metricsOverflow">If true, indicates overflow flush (don't clear dimensions)</param>
    private void FlushContext(MetricsContext context, bool metricsOverflow)
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

    /// <inheritdoc />
    void IMetrics.ClearDefaultDimensions()
    {
        // Clear shared default dimensions
        lock (_defaultDimensionsLock)
        {
            _sharedDefaultDimensions.Clear();
        }
        
        // Clear in all existing thread contexts
        foreach (var kvp in _threadContexts)
        {
            kvp.Value.ClearDefaultDimensions();
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
            // Store in shared state for new thread contexts
            _sharedService = parsedService;
            
            // Add Service to shared default dimensions
            lock (_defaultDimensionsLock)
            {
                // Remove existing Service dimension if present
                _sharedDefaultDimensions.RemoveAll(d => d.Dimensions.ContainsKey("Service"));
                // Add new Service dimension
                _sharedDefaultDimensions.Add(new DimensionSet("Service", parsedService));
            }
            
            // Update current thread's context
            var context = CurrentContext;
            context.SetService(parsedService);
            
            // Update default dimensions in current context with the shared list
            lock (_defaultDimensionsLock)
            {
                context.SetDefaultDimensions(new List<DimensionSet>(_sharedDefaultDimensions));
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
            return CurrentContext.GetNamespace() ?? _powertoolsConfigurations.MetricsNamespace;
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
        
        // bring default dimensions if exist
        var dimensions = Options?.DefaultDimensions;
        
        var functionName = Options?.FunctionName ?? context?.FunctionName ?? "";
        if (!string.IsNullOrWhiteSpace(functionName))
        {
            dimensions ??= new Dictionary<string, string>();
            dimensions.Add("FunctionName", functionName);
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

        // Add the dimensionSet to current thread's context
        CurrentContext.AddDimensions([dimensionSet]);
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
        _threadContexts.Clear();
        _sharedNamespace = null;
        _sharedService = null;
        lock (_defaultDimensionsLock)
        {
            _sharedDefaultDimensions.Clear();
        }
    }

    /// <summary>
    ///     Clears the current thread's context. Useful for cleanup after each Lambda invocation.
    /// </summary>
    internal static void ClearCurrentThreadContext()
    {
        var threadId = Environment.CurrentManagedThreadId;
        _threadContexts.TryRemove(threadId, out _);
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