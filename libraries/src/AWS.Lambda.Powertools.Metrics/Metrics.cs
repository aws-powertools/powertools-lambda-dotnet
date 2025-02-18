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
    private readonly bool _raiseOnEmptyMetrics;

    /// <summary>
    ///     The capture cold start enabled
    /// </summary>
    private readonly bool _captureColdStartEnabled;

    // <summary>
    // Shared synchronization object
    // </summary>
    private readonly object _lockObj = new();

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
    internal Metrics(IPowertoolsConfigurations powertoolsConfigurations, string nameSpace = null, string service = null,
        bool raiseOnEmptyMetrics = false, bool captureColdStartEnabled = false)
    {
        _instance ??= this;

        _powertoolsConfigurations = powertoolsConfigurations;
        _raiseOnEmptyMetrics = raiseOnEmptyMetrics;
        _captureColdStartEnabled = captureColdStartEnabled;
        _context = InitializeContext(nameSpace, service, null);

        _powertoolsConfigurations.SetExecutionEnvironment(this);
    }

    /// <summary>
    ///     Implements interface that adds new metric to memory.
    /// </summary>
    /// <param name="key">Metric Key</param>
    /// <param name="value">Metric Value</param>
    /// <param name="unit">Metric Unit</param>
    /// <param name="metricResolution">Metric resolution</param>
    /// <exception cref="System.ArgumentNullException">
    ///     'AddMetric' method requires a valid metrics key. 'Null' or empty values
    ///     are not allowed.
    /// </exception>
    void IMetrics.AddMetric(string key, double value, MetricUnit unit, MetricResolution metricResolution)
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
                _instance.Flush(true);
            }

            _context.AddMetric(key, value, unit, metricResolution);
        }
    }

    /// <summary>
    ///     Implements interface that sets metrics namespace identifier.
    /// </summary>
    /// <param name="nameSpace">Metrics Namespace Identifier</param>
    void IMetrics.SetNamespace(string nameSpace)
    {
        _context.SetNamespace(nameSpace);
    }

    /// <summary>
    ///     Implements interface that allows retrieval of namespace identifier.
    /// </summary>
    /// <returns>Namespace identifier</returns>
    string IMetrics.GetNamespace()
    {
        try
        {
            return _context.GetNamespace();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Implements interface to get service name
    /// </summary>
    /// <returns>System.String.</returns>
    string IMetrics.GetService()
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

    /// <summary>
    ///     Implements interface that adds a dimension.
    /// </summary>
    /// <param name="key">Dimension key. Must not be null, empty or whitespace</param>
    /// <param name="value">Dimension value</param>
    /// <exception cref="System.ArgumentNullException">
    ///     'AddDimension' method requires a valid dimension key. 'Null' or empty
    ///     values are not allowed.
    /// </exception>
    void IMetrics.AddDimension(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key),
                "'AddDimension' method requires a valid dimension key. 'Null' or empty values are not allowed.");

        _context.AddDimension(key, value);
    }

    /// <summary>
    ///     Implements interface that adds metadata.
    /// </summary>
    /// <param name="key">Metadata key. Must not be null, empty or whitespace</param>
    /// <param name="value">Metadata value</param>
    /// <exception cref="System.ArgumentNullException">
    ///     'AddMetadata' method requires a valid metadata key. 'Null' or empty
    ///     values are not allowed.
    /// </exception>
    void IMetrics.AddMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key),
                "'AddMetadata' method requires a valid metadata key. 'Null' or empty values are not allowed.");

        _context.AddMetadata(key, value);
    }

    /// <summary>
    ///     Implements interface that sets default dimension list
    /// </summary>
    /// <param name="defaultDimension">Default Dimension List</param>
    /// <exception cref="System.ArgumentNullException">
    ///     'SetDefaultDimensions' method requires a valid key pair. 'Null' or empty
    ///     values are not allowed.
    /// </exception>
    void IMetrics.SetDefaultDimensions(Dictionary<string, string> defaultDimension)
    {
        foreach (var item in defaultDimension)
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                throw new ArgumentNullException(nameof(item.Key),
                    "'SetDefaultDimensions' method requires a valid key pair. 'Null' or empty values are not allowed.");

        _context.SetDefaultDimensions(DictionaryToList(defaultDimension));
    }

    /// <summary>
    ///     Flushes metrics in Embedded Metric Format (EMF) to Standard Output. In Lambda, this output is collected
    ///     automatically and sent to Cloudwatch.
    /// </summary>
    /// <param name="metricsOverflow">If enabled, non-default dimensions are cleared after flushing metrics</param>
    /// <exception cref="SchemaValidationException">true</exception>
    void IMetrics.Flush(bool metricsOverflow)
    {
        if (_context.GetMetrics().Count == 0
            && _raiseOnEmptyMetrics)
            throw new SchemaValidationException(true);

        if (_context.IsSerializable)
        {
            var emfPayload = _context.Serialize();

            Console.WriteLine(emfPayload);

            _context.ClearMetrics();

            if (!metricsOverflow) _context.ClearNonDefaultDimensions();
        }
        else
        {
            if (!_captureColdStartEnabled)
                Console.WriteLine(
                    "##User-WARNING## No application metrics to publish. The cold-start metric may be published if enabled. If application metrics should never be empty, consider using 'RaiseOnEmptyMetrics = true'");
        }
    }
    
    /// <summary>
    ///     Clears both default dimensions and dimensions lists
    /// </summary>
    void IMetrics.ClearDefaultDimensions()
    {
        _context.ClearDefaultDimensions();
    }

    /// <summary>
    ///     Serialize global context object
    /// </summary>
    /// <returns>Serialized global context object</returns>
    public string Serialize()
    {
        return _context.Serialize();
    }

    /// <summary>
    ///     Implements the interface that pushes single metric to CloudWatch using Embedded Metric Format. This can be used to
    ///     push metrics with a different context.
    /// </summary>
    /// <param name="metricName">Metric Name. Metric key cannot be null, empty or whitespace</param>
    /// <param name="value">Metric Value</param>
    /// <param name="unit">Metric Unit</param>
    /// <param name="nameSpace">Metric Namespace</param>
    /// <param name="service">Service Name</param>
    /// <param name="defaultDimensions">Default dimensions list</param>
    /// <param name="metricResolution">Metrics resolution</param>
    /// <exception cref="System.ArgumentNullException">
    ///     'PushSingleMetric' method requires a valid metrics key. 'Null' or empty
    ///     values are not allowed.
    /// </exception>
    void IMetrics.PushSingleMetric(string metricName, double value, MetricUnit unit, string nameSpace, string service,
        Dictionary<string, string> defaultDimensions, MetricResolution metricResolution)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentNullException(nameof(metricName),
                "'PushSingleMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.");

        using var context = InitializeContext(nameSpace, service, defaultDimensions);
        context.AddMetric(metricName, value, unit, metricResolution);

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
            _instance.Flush();
        }
    }

    /// <summary>
    ///     Adds new metric to memory.
    /// </summary>
    /// <param name="key">Metric Key. Must not be null, empty or whitespace</param>
    /// <param name="value">Metric Value</param>
    /// <param name="unit">Metric Unit</param>
    /// <param name="metricResolution"></param>
    public static void AddMetric(string key, double value, MetricUnit unit = MetricUnit.None,
        MetricResolution metricResolution = MetricResolution.Default)
    {
        _instance.AddMetric(key, value, unit, metricResolution);
    }

    /// <summary>
    ///     Sets metrics namespace identifier.
    /// </summary>
    /// <param name="nameSpace">Metrics Namespace Identifier</param>
    public static void SetNamespace(string nameSpace)
    {
        _instance.SetNamespace(nameSpace);
    }

    /// <summary>
    ///     Retrieves namespace identifier.
    /// </summary>
    /// <returns>Namespace identifier</returns>
    public static string GetNamespace()
    {
        return _instance.GetNamespace();
    }

    /// <summary>
    ///     Adds new dimension to memory.
    /// </summary>
    /// <param name="key">Dimension key. Must not be null, empty or whitespace.</param>
    /// <param name="value">Dimension value</param>
    public static void AddDimension(string key, string value)
    {
        _instance.AddDimension(key, value);
    }

    /// <summary>
    ///     Adds metadata to memory.
    /// </summary>
    /// <param name="key">Metadata key. Must not be null, empty or whitespace</param>
    /// <param name="value">Metadata value</param>
    public static void AddMetadata(string key, object value)
    {
        _instance.AddMetadata(key, value);
    }

    /// <summary>
    ///     Set default dimension list
    /// </summary>
    /// <param name="defaultDimensions">Default Dimension List</param>
    public static void SetDefaultDimensions(Dictionary<string, string> defaultDimensions)
    {
        _instance.SetDefaultDimensions(defaultDimensions);
    }

    /// <summary>
    ///     Clears both default dimensions and dimensions lists
    /// </summary>
    public static void ClearDefaultDimensions()
    {
        _instance.ClearDefaultDimensions();
    }

    /// <summary>
    ///     Flushes metrics in Embedded Metric Format (EMF) to Standard Output. In Lambda, this output is collected
    ///     automatically and sent to Cloudwatch.
    /// </summary>
    /// <param name="context">If context is provided it is serialized instead of the global context object</param>
    private void Flush(MetricsContext context)
    {
        var emfPayload = context.Serialize();

        Console.WriteLine(emfPayload);
    }

    /// <summary>
    ///     Pushes single metric to CloudWatch using Embedded Metric Format. This can be used to push metrics with a different
    ///     context.
    /// </summary>
    /// <param name="metricName">Metric Name. Metric key cannot be null, empty or whitespace</param>
    /// <param name="value">Metric Value</param>
    /// <param name="unit">Metric Unit</param>
    /// <param name="nameSpace">Metric Namespace</param>
    /// <param name="service">Service Name</param>
    /// <param name="defaultDimensions">Default dimensions list</param>
    /// <param name="metricResolution">Metrics resolution</param>
    public static void PushSingleMetric(string metricName, double value, MetricUnit unit, string nameSpace = null,
        string service = null, Dictionary<string, string> defaultDimensions = null,
        MetricResolution metricResolution = MetricResolution.Default)
    {
        _instance.PushSingleMetric(metricName, value, unit, nameSpace, service, defaultDimensions, metricResolution);
    }

    /// <summary>
    ///     Sets global namespace, service name and default dimensions list. 
    /// </summary>
    /// <param name="nameSpace">Metrics namespace</param>
    /// <param name="service">Service Name</param>
    /// <param name="defaultDimensions">Default Dimensions List</param>
    /// <returns>MetricsContext.</returns>
    private MetricsContext InitializeContext(string nameSpace, string service,
        Dictionary<string, string> defaultDimensions)
    {
        var context = new MetricsContext();
        var defaultDimensionsList = DictionaryToList(defaultDimensions);

        context.SetNamespace(!string.IsNullOrWhiteSpace(nameSpace)
            ? nameSpace
            :  _instance.GetNamespace() ?? _powertoolsConfigurations.MetricsNamespace);

        // this needs to check if service is set through code or env variables
        // the default value service_undefined has to be ignored and return null so it is not added as default
        // TODO: Check if there is a way to get the default dimensions and if it makes sense     
        var parsedService = !string.IsNullOrWhiteSpace(service)
            ? service
            : _powertoolsConfigurations.Service == "service_undefined"
                ? null
                : _powertoolsConfigurations.Service;

        if (parsedService != null)
        {
            context.SetService(parsedService);
            defaultDimensionsList.Add(new DimensionSet("Service", context.GetService()));
        }

        context.SetDefaultDimensions(defaultDimensionsList);

        return context;
    }

    /// <summary>
    ///     Helper method to convert default dimensions dictionary to list
    /// </summary>
    /// <param name="defaultDimensions">Default dimensions dictionary</param>
    /// <returns>Default dimensions list</returns>
    private List<DimensionSet> DictionaryToList(Dictionary<string, string> defaultDimensions)
    {
        var defaultDimensionsList = new List<DimensionSet>();
        if (defaultDimensions != null)
            foreach (var item in defaultDimensions)
                defaultDimensionsList.Add(new DimensionSet(item.Key, item.Value));

        return defaultDimensionsList;
    }

    /// <summary>
    ///     Helper method for testing purposes. Clears static instance between test execution
    /// </summary>
    internal static void ResetForTest()
    {
        _instance = null;
    }
}