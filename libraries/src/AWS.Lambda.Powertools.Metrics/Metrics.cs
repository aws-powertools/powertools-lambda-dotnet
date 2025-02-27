/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
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
    ///    Gets or sets the instance.
    /// </summary>
    public static IMetrics Instance
    {
        get => _instance ?? new Metrics(PowertoolsConfigurations.Instance, consoleWrapper: new ConsoleWrapper());
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
    private static IMetrics _instance;

    /// <summary>
    ///     The context
    /// </summary>
    private readonly MetricsContext _context;

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

        if (!string.IsNullOrEmpty(options.Service))
            Instance.SetService(options.Service);

        if (options.RaiseOnEmptyMetrics.HasValue)
            Instance.SetRaiseOnEmptyMetrics(options.RaiseOnEmptyMetrics.Value);
        if (options.CaptureColdStart.HasValue)
            Instance.SetCaptureColdStart(options.CaptureColdStart.Value);

        if (options.DefaultDimensions != null)
            SetDefaultDimensions(options.DefaultDimensions);

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
        _context = new MetricsContext();
        _raiseOnEmptyMetrics = raiseOnEmptyMetrics;
        _captureColdStartEnabled = captureColdStartEnabled;
        _options = options;

        Instance = this;
        _powertoolsConfigurations.SetExecutionEnvironment(this);

        if (!string.IsNullOrEmpty(nameSpace)) SetNamespace(nameSpace);
        if (!string.IsNullOrEmpty(service)) SetService(service);
    }

    /// <inheritdoc />
    void IMetrics.AddMetric(string key, double value, MetricUnit unit, MetricResolution resolution)
    {
        if (Instance != null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(
                    nameof(key),
                    "'AddMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.");

            if (value < 0)
            {
                throw new ArgumentException(
                    "'AddMetric' method requires a valid metrics value. Value must be >= 0.", nameof(value));
            }

            lock (_lockObj)
            {
                var metrics = _context.GetMetrics();

                if (metrics.Count > 0 &&
                    (metrics.Count == PowertoolsConfigurations.MaxMetrics ||
                     metrics.FirstOrDefault(x => x.Name == key)
                         ?.Values.Count == PowertoolsConfigurations.MaxMetrics))
                {
                    Instance.Flush(true);
                }

                _context.AddMetric(key, value, unit, resolution);
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
        _context.SetNamespace(!string.IsNullOrWhiteSpace(nameSpace)
            ? nameSpace
            : GetNamespace() ?? _powertoolsConfigurations.MetricsNamespace);
    }


    /// <summary>
    ///     Implements interface to get service name
    /// </summary>
    /// <returns>System.String.</returns>
    private string GetService()
    {
        try
        {
            return _context.GetService();
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

        _context.AddDimension(key, value);
    }

    /// <inheritdoc />
    void IMetrics.AddMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key),
                "'AddMetadata' method requires a valid metadata key. 'Null' or empty values are not allowed.");

        _context.AddMetadata(key, value);
    }

    /// <inheritdoc />
    void IMetrics.SetDefaultDimensions(Dictionary<string, string> defaultDimensions)
    {
        foreach (var item in defaultDimensions)
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                throw new ArgumentNullException(nameof(item.Key),
                    "'SetDefaultDimensions' method requires a valid key pair. 'Null' or empty values are not allowed.");

        _context.SetDefaultDimensions(DictionaryToList(defaultDimensions));
    }

    /// <inheritdoc />
    void IMetrics.Flush(bool metricsOverflow)
    {
        if (_context.GetMetrics().Count == 0
            && _raiseOnEmptyMetrics)
            throw new SchemaValidationException(true);

        if (_context.IsSerializable)
        {
            var emfPayload = _context.Serialize();

            _consoleWrapper.WriteLine(emfPayload);

            _context.ClearMetrics();

            if (!metricsOverflow) _context.ClearNonDefaultDimensions();
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
        _context.ClearDefaultDimensions();
    }

    /// <inheritdoc />
    public void SetService(string service)
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
            _context.SetService(parsedService);
            _context.SetDefaultDimensions(new List<DimensionSet>(new[]
                { new DimensionSet("Service", GetService()) }));
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
        return ListToDictionary(_context.GetDefaultDimensions());
    }

    /// <inheritdoc />
    void IMetrics.PushSingleMetric(string name, double value, MetricUnit unit, string nameSpace,
        string service, Dictionary<string, string> dimensions, MetricResolution resolution)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name),
                "'PushSingleMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.");

        var context = new MetricsContext();
        context.SetNamespace(nameSpace ?? GetNamespace());
        context.SetService(service ?? _context.GetService());

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
    ///     Retrieves namespace identifier.
    /// </summary>
    /// <returns>Namespace identifier</returns>
    public string GetNamespace()
    {
        try
        {
            return _context.GetNamespace() ?? _powertoolsConfigurations.MetricsNamespace;
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
        try
        {
            return dimensions != null
                ? new Dictionary<string, string>(dimensions.SelectMany(x => x.Dimensions))
                : dictionary;
        }
        catch (Exception e)
        {
            _consoleWrapper.Debug("Error converting list to dictionary: " + e.Message);
            return dictionary;
        }
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

    /// <summary>
    ///     Helper method for testing purposes. Clears static instance between test execution
    /// </summary>
    internal static void ResetForTest()
    {
        Instance = null;
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