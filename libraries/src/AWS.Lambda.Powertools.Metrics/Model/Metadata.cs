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
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Class Metadata.
/// </summary>
public class Metadata
{
    /// <summary>
    ///     Create Metadata object
    /// </summary>
    public Metadata()
    {
        CloudWatchMetrics = new List<MetricDirective> {new()};
        CustomMetadata = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Gets the timestamp.
    /// </summary>
    /// <value>The timestamp.</value>
    [JsonConverter(typeof(UnixMillisecondDateTimeConverter))]
    public DateTime Timestamp => DateTime.Now;

    /// <summary>
    ///     Gets the cloud watch metrics.
    /// </summary>
    /// <value>The cloud watch metrics.</value>
    [JsonPropertyName(nameof(CloudWatchMetrics))]
    public List<MetricDirective> CloudWatchMetrics { get; }

    /// <summary>
    ///     Gets the metric directive.
    /// </summary>
    /// <value>The metric directive.</value>
    private MetricDirective _metricDirective => CloudWatchMetrics[0];

    /// <summary>
    ///     Gets the custom metadata.
    /// </summary>
    /// <value>The custom metadata.</value>
    [JsonIgnore]
    public Dictionary<string, object> CustomMetadata { get; }

    /// <summary>
    ///     Deletes all metrics from memory
    /// </summary>
    internal void ClearMetrics()
    {
        _metricDirective.Metrics.Clear();
    }

    /// <summary>
    ///     Deletes non-default dimensions from memory
    /// </summary>
    internal void ClearNonDefaultDimensions()
    {
        _metricDirective.Dimensions.Clear();
    }

    /// <summary>
    ///     Adds metric to memory
    /// </summary>
    /// <param name="key">Metric key. Cannot be null, empty or whitespace</param>
    /// <param name="value">Metric value</param>
    /// <param name="unit">Metric Unit</param>
    /// <param name="metricResolution">Metric Resolution, Standard (default), High</param>
    internal void AddMetric(string key, double value, MetricUnit unit, MetricResolution metricResolution)
    {
        _metricDirective.AddMetric(key, value, unit, metricResolution);
    }

    /// <summary>
    ///     Sets global metrics namespace
    /// </summary>
    /// <param name="metricNamespace">Global metrics namespace</param>
    internal void SetNamespace(string metricNamespace)
    {
        _metricDirective.SetNamespace(metricNamespace);
    }

    /// <summary>
    ///     Retrieves global namespace identifier
    /// </summary>
    /// <returns>Global namespace identifier</returns>
    internal string GetNamespace()
    {
        return _metricDirective.Namespace;
    }

    /// <summary>
    ///     Sets service name
    /// </summary>
    /// <param name="service">Service name</param>
    internal void SetService(string service)
    {
        _metricDirective.SetService(service);
    }

    /// <summary>
    ///     Retrieves service name
    /// </summary>
    /// <returns>Service name</returns>
    internal string GetService()
    {
        return _metricDirective.Service;
    }

    /// <summary>
    ///     Adds new Dimension
    /// </summary>
    /// <param name="dimension">Dimension to add</param>
    internal void AddDimensionSet(DimensionSet dimension)
    {
        _metricDirective.AddDimension(dimension);
    }

    /// <summary>
    ///     Sets default dimensions list
    /// </summary>
    /// <param name="defaultDimensionSets">Default dimensions list</param>
    internal void SetDefaultDimensions(List<DimensionSet> defaultDimensionSets)
    {
        _metricDirective.SetDefaultDimensions(defaultDimensionSets);
    }

    /// <summary>
    ///     Retrieves metrics stored in memory
    /// </summary>
    /// <returns>List of metrics stored in memory</returns>
    internal List<MetricDefinition> GetMetrics()
    {
        return _metricDirective.Metrics;
    }

    /// <summary>
    ///     Adds metadata to memory
    /// </summary>
    /// <param name="key">Metadata key. Cannot be null, empty or whitespace</param>
    /// <param name="value">Metadata value</param>
    internal void AddMetadata(string key, object value)
    {
        CustomMetadata.Add(key, value);
    }

    /// <summary>
    ///     Creates Dictionary with all Dimensions. Needed for correct EMF payload serialization
    /// </summary>
    /// <returns>Dictionary with all dimensions</returns>
    internal Dictionary<string, string> ExpandAllDimensionSets()
    {
        Dictionary<string, string> dimensionSets;
        if (CloudWatchMetrics.Count > 0)
            dimensionSets = _metricDirective.ExpandAllDimensionSets();
        else
            dimensionSets = new Dictionary<string, string>();

        return dimensionSets;
    }
}