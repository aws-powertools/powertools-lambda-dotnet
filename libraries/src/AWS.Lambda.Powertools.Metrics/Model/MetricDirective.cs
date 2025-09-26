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
            var result = new List<List<string>>();
            var allDimKeys = new List<string>();

            // Create snapshots to avoid concurrent modification issues
            var defaultDimensionsSnapshot = new List<DimensionSet>(DefaultDimensions);
            var dimensionsSnapshot = new List<DimensionSet>(Dimensions);

            // Add default dimensions keys
            foreach (var dimensionSet in defaultDimensionsSnapshot)
            {
                var keysSnapshot = dimensionSet.DimensionKeys;
                foreach (var key in keysSnapshot)
                {
                    if (!allDimKeys.Contains(key))
                    {
                        allDimKeys.Add(key);
                    }
                }
            }

            // Add all regular dimensions to the same array
            foreach (var dimensionSet in dimensionsSnapshot)
            {
                var keysSnapshot = dimensionSet.DimensionKeys;
                foreach (var key in keysSnapshot)
                {
                    if (!allDimKeys.Contains(key))
                    {
                        allDimKeys.Add(key);
                    }
                }
            }

            // Add non-empty dimension arrays
            // When no dimensions exist, add an empty array
            result.Add(allDimKeys.Count > 0 ? allDimKeys : []);

            return result;
        }
    }
    
    /// <summary>
    /// Shared synchronization object
    /// </summary>
    internal readonly object _lockObj = new();

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
        if (string.IsNullOrEmpty(name))
            return;
            
        lock (_lockObj)
        {
            if (Metrics.Count >= PowertoolsConfigurations.MaxMetrics)
            {
                throw new ArgumentOutOfRangeException(nameof(Metrics),
                    $"Cannot add more than {PowertoolsConfigurations.MaxMetrics} metrics at the same time.");
            }
            
            var metric = GetExistingMetric(Metrics, name);
            if (metric?.Values != null)
            {
                if (metric.Values.Count < PowertoolsConfigurations.MaxMetrics)
                {
                    metric.AddValue(value);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(metric),
                        $"Cannot add more than {PowertoolsConfigurations.MaxMetrics} metric data points at the same time.");
                }
            }
            else
            {
                // Either no existing metric found or metric/Values became null due to concurrent access
                Metrics.Add(new MetricDefinition(name, unit, value, metricResolution));
            }
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
        if (dimension?.Dimensions == null)
            return;
            
        lock (_lockObj)
        {
            // Check if we already have any dimensions
            if (Dimensions.Count > 0)
            {
                // Get the first dimension set where we now store all dimensions
                var firstDimensionSet = Dimensions[0];
                
                // Null check for thread safety
                if (firstDimensionSet?.Dimensions == null)
                {
                    // If first dimension set is null, replace it with the new one
                    Dimensions[0] = dimension;
                    return;
                }
            
                // Check the actual dimension count inside the first dimension set
                if (firstDimensionSet.Dimensions.Count >= PowertoolsConfigurations.MaxDimensions)
                {
                    throw new ArgumentOutOfRangeException(nameof(dimension),
                        $"Cannot add more than {PowertoolsConfigurations.MaxDimensions} dimensions at the same time.");
                }

                // Add to the first dimension set instead of creating a new one
                // Create a snapshot to avoid concurrent modification issues
                var dimensionSnapshot = new Dictionary<string, string>(dimension.Dimensions);
                foreach (var pair in dimensionSnapshot)
                {
                    if (!firstDimensionSet.Dimensions.ContainsKey(pair.Key))
                    {
                        firstDimensionSet.Dimensions.Add(pair.Key, pair.Value);
                    }
                    else
                    {
                        Console.WriteLine(
                            $"##WARNING##: Failed to Add dimension '{pair.Key}'. Dimension already exists.");
                    }
                }
            }
            else
            {
                // No dimensions yet, add the new one
                Dimensions.Add(dimension);
            }
        }
    }

    /// <summary>
    ///     Sets default dimensions
    /// </summary>
    /// <param name="defaultDimensions">Default dimensions list</param>
    internal void SetDefaultDimensions(List<DimensionSet> defaultDimensions)
    {
        if (DefaultDimensions.Count == 0)
            DefaultDimensions = defaultDimensions;
        else
        {
            foreach (var item in defaultDimensions)
            {
                if (item.DimensionKeys.Count == 0)
                    continue;
                    
                bool exists = false;
                var itemFirstKey = item.DimensionKeys[0];
                
                foreach (var existing in DefaultDimensions)
                {
                    var existingKeys = existing.DimensionKeys;
                    for (int i = 0; i < existingKeys.Count; i++)
                    {
                        if (existingKeys[i] == itemFirstKey)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists) break;
                }
                if (!exists)
                    DefaultDimensions.Add(item);
            }
        }
    }

    /// <summary>
    ///     Appends dimension and default dimension lists
    /// </summary>
    /// <returns>Dictionary with dimension and default dimension list appended</returns>
    internal Dictionary<string, string> ExpandAllDimensionSets()
    {
        // if a key appears multiple times, the last value will be the one that's used in the output.
        var dimensions = new Dictionary<string, string>();

        // Create snapshots to avoid concurrent modification issues
        var defaultDimensionsSnapshot = new List<DimensionSet>(DefaultDimensions);
        var dimensionsSnapshot = new List<DimensionSet>(Dimensions);

        foreach (var dimensionSet in defaultDimensionsSnapshot)
        {
            if (dimensionSet?.Dimensions != null)
            {
                var dimensionSnapshot = new Dictionary<string, string>(dimensionSet.Dimensions);
                foreach (var (key, value) in dimensionSnapshot)
                    dimensions[key] = value;
            }
        }

        foreach (var dimensionSet in dimensionsSnapshot)
        {
            if (dimensionSet?.Dimensions != null)
            {
                var dimensionSnapshot = new Dictionary<string, string>(dimensionSet.Dimensions);
                foreach (var (key, value) in dimensionSnapshot)
                    dimensions[key] = value;
            }
        }

        return dimensions;
    }
    
    /// <summary>
    ///     Adds multiple dimensions as a complete dimension set to memory.
    /// </summary>
    /// <param name="dimensionSets">List of dimension sets to add</param>
    internal void AddDimensionSet(List<DimensionSet> dimensionSets)
    {
        if (dimensionSets == null || dimensionSets.Count == 0)
            return;

        if (Dimensions.Count + dimensionSets.Count <= PowertoolsConfigurations.MaxDimensions)
        {
            // Simply add the dimension sets without checking for existing keys
            // This ensures dimensions added together stay together
            foreach (var dimensionSet in dimensionSets)
            {
                if (dimensionSet.DimensionKeys.Count > 0)
                {
                    Dimensions.Add(dimensionSet);
                }
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(Dimensions),
                $"Cannot add more than {PowertoolsConfigurations.MaxDimensions} dimensions at the same time.");
        }
    }

    /// <summary>
    ///     Safely searches for an existing metric by name without using LINQ enumeration
    /// </summary>
    /// <param name="metrics">The metrics collection to search</param>
    /// <param name="name">The metric name to search for</param>
    /// <returns>The found metric or null if not found</returns>
    private static MetricDefinition GetExistingMetric(List<MetricDefinition> metrics, string name)
    {
        // Use a traditional for loop instead of LINQ to avoid enumeration issues
        // when the collection is modified concurrently
        if (metrics == null || string.IsNullOrEmpty(name))
            return null;
            
        // Create a snapshot of the count to avoid issues with concurrent modifications
        var count = metrics.Count;
        for (int i = 0; i < count; i++)
        {
            try
            {
                // Check bounds again in case collection was modified
                if (i >= metrics.Count)
                    break;
                    
                var metric = metrics[i];
                if (metric != null && string.Equals(metric.Name, name, StringComparison.Ordinal))
                {
                    return metric;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // Collection was modified during iteration, return null to be safe
                break;
            }
            catch (IndexOutOfRangeException)
            {
                // Collection was modified during iteration, return null to be safe
                break;
            }
        }
        return null;
    }

    /// <summary>
    ///     Clears both default dimensions and dimensions lists
    /// </summary>
    internal void ClearDefaultDimensions()
    {
        DefaultDimensions.Clear();
    }
}