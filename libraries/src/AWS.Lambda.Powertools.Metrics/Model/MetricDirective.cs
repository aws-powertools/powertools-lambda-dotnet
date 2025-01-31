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
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Class MetricDirective.
/// </summary>
public class MetricDirective
{
    /// <summary>
    ///     Creates empty MetricDirective object
    /// </summary>
    public MetricDirective() : this(null, new List<MetricDefinition>(), new List<DimensionSet>())
    {
    }

    /// <summary>
    ///     Creates MetricDirective object with specific namespace identifier
    /// </summary>
    /// <param name="nameSpace">Metrics namespace identifier</param>
    public MetricDirective(string nameSpace) : this(nameSpace, new List<MetricDefinition>(), new List<DimensionSet>())
    {
    }

    /// <summary>
    ///     Creates MetricDirective object with specific namespace identifier and default dimensions list
    /// </summary>
    /// <param name="nameSpace">Metrics namespace identifier</param>
    /// <param name="defaultDimensions">Default dimensions list</param>
    public MetricDirective(string nameSpace, List<DimensionSet> defaultDimensions) : this(nameSpace,
        new List<MetricDefinition>(), defaultDimensions)
    {
    }

    /// <summary>
    ///     Creates MetricDirective object with specific namespace identifier, list of metrics and default dimensions list
    /// </summary>
    /// <param name="nameSpace">Metrics namespace identifier</param>
    /// <param name="metrics">List of metrics</param>
    /// <param name="defaultDimensions">Default dimensions list</param>
    private MetricDirective(string nameSpace, List<MetricDefinition> metrics, List<DimensionSet> defaultDimensions)
    {
        Namespace = nameSpace;
        Metrics = metrics;
        Dimensions = new List<DimensionSet>();
        DefaultDimensions = defaultDimensions;
    }

    /// <summary>
    ///     Gets the namespace.
    /// </summary>
    /// <value>The namespace.</value>
    [JsonPropertyName(nameof(Namespace))]
    public string Namespace { get; private set; }

    /// <summary>
    ///     Gets the service.
    /// </summary>
    /// <value>The service.</value>
    [JsonIgnore]
    public string Service { get; private set; }

    /// <summary>
    ///     Gets the metrics.
    /// </summary>
    /// <value>The metrics.</value>
    [JsonPropertyName(nameof(Metrics))]
    public List<MetricDefinition> Metrics { get; private set; }

    /// <summary>
    ///     Gets the dimensions.
    /// </summary>
    /// <value>The dimensions.</value>
    [JsonIgnore]
    public List<DimensionSet> Dimensions { get; private set; }

    /// <summary>
    ///     Gets the default dimensions.
    /// </summary>
    /// <value>The default dimensions.</value>
    [JsonIgnore]
    public List<DimensionSet> DefaultDimensions { get; private set; }

    /// <summary>
    ///     Creates list with all dimensions. Needed for correct EMF payload creation
    /// </summary>
    /// <value>All dimension keys.</value>
    [JsonPropertyName("Dimensions")]
    public List<List<string>> AllDimensionKeys
    {
        get
        {
            var defaultKeys = DefaultDimensions
                .Where(d => d.DimensionKeys.Any())
                .SelectMany(s => s.DimensionKeys)
                .ToList();

            var keys = Dimensions
                .Where(d => d.DimensionKeys.Any())
                .SelectMany(s => s.DimensionKeys)
                .ToList();

            defaultKeys.AddRange(keys);

            if (defaultKeys.Count == 0) defaultKeys = new List<string>();

            // Wrap the list of strings in another list
            return new List<List<string>> { defaultKeys };
        }
    }
    
    /// <summary>
    /// Shared synchronization object
    /// </summary>
    private readonly object _lockObj = new();

    /// <summary>
    ///     Adds metric to memory
    /// </summary>
    /// <param name="name">Metric name. Cannot be null, empty or whitespace</param>
    /// <param name="value">Metric value</param>
    /// <param name="unit">Metric unit</param>
    /// <param name="metricResolution">Metric Resolution, Standard (default), High</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Metrics - Cannot add more than 100 metrics at the same time.</exception>
    public void AddMetric(string name, double value, MetricUnit unit, MetricResolution metricResolution)
    {
        if (Metrics.Count < PowertoolsConfigurations.MaxMetrics)
        {
            lock (_lockObj)
            {
                var metric = Metrics.FirstOrDefault(m => m.Name == name);
                if (metric != null)
                {
                    if (metric.Values.Count < PowertoolsConfigurations.MaxMetrics)
                        metric.AddValue(value);
                    else
                        throw new ArgumentOutOfRangeException(nameof(metric),
                            $"Cannot add more than {PowertoolsConfigurations.MaxMetrics} metric data points at the same time.");
                }
                else
                    Metrics.Add(new MetricDefinition(name, unit, value, metricResolution));
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(Metrics),
                $"Cannot add more than {PowertoolsConfigurations.MaxMetrics} metrics at the same time.");
        }
    }

    /// <summary>
    ///     Sets metrics namespace identifier
    /// </summary>
    /// <param name="nameSpace">Metrics namespace identifier</param>
    internal void SetNamespace(string nameSpace)
    {
        Namespace = nameSpace;
    }

    /// <summary>
    ///     Sets service name
    /// </summary>
    /// <param name="service">Service name</param>
    internal void SetService(string service)
    {
        Service = service;
    }

    /// <summary>
    ///     Adds new dimension to memory
    /// </summary>
    /// <param name="dimension">Metrics Dimension</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Dimensions - Cannot add more than 9 dimensions at the same time.</exception>
    internal void AddDimension(DimensionSet dimension)
    {
        if (Dimensions.Count < PowertoolsConfigurations.MaxDimensions)
        {
            var matchingKeys = AllDimensionKeys.Where(x => x.Contains(dimension.DimensionKeys[0]));
            if (!matchingKeys.Any())
                Dimensions.Add(dimension);
            else
                Console.WriteLine(
                    $"##WARNING##: Failed to Add dimension '{dimension.DimensionKeys[0]}'. Dimension already exists.");
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(Dimensions),
                "Cannot add more than 9 dimensions at the same time.");
        }
    }

    /// <summary>
    ///     Sets default dimensions
    /// </summary>
    /// <param name="defaultDimensions">Default dimensions list</param>
    internal void SetDefaultDimensions(List<DimensionSet> defaultDimensions)
    {
        if (!DefaultDimensions.Any())
            DefaultDimensions = defaultDimensions;
        else
            foreach (var item in defaultDimensions)
                if (!DefaultDimensions.Any(d => d.DimensionKeys.Contains(item.DimensionKeys[0])))
                    DefaultDimensions.Add(item);
    }

    /// <summary>
    ///     Appends dimension and default dimension lists
    /// </summary>
    /// <returns>Dictionary with dimension and default dimension list appended</returns>
    internal Dictionary<string, string> ExpandAllDimensionSets()
    {
        var dimensions = new Dictionary<string, string>();

        foreach (var dimensionSet in DefaultDimensions)
        foreach (var (key, value) in dimensionSet.Dimensions)
            dimensions.TryAdd(key, value);

        foreach (var dimensionSet in Dimensions)
        foreach (var (key, value) in dimensionSet.Dimensions)
            dimensions.TryAdd(key, value);

        return dimensions;
    }

    /// <summary>
    ///     Clears both default dimensions and dimensions lists
    /// </summary>
    internal void ClearDefaultDimensions()
    {
        DefaultDimensions.Clear();
    }
}