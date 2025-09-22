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

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Class RootNode.
/// </summary>
public class RootNode
{
    /// <summary>
    ///     Gets the aws.
    /// </summary>
    /// <value>The aws.</value>
    [JsonPropertyName("_aws")]
    public Metadata AWS { get; } = new();

    /// <summary>
    ///     Gets the metric data.
    /// </summary>
    /// <value>The metric data.</value>
    [JsonExtensionData]
    public Dictionary<string, object> MetricData
    {
        get
        {
            var targetMembers = new Dictionary<string, object>();

            // Create snapshots to avoid concurrent modification issues
            var dimensionsSnapshot = AWS.ExpandAllDimensionSets();
            foreach (var dimension in dimensionsSnapshot) 
                targetMembers.Add(dimension.Key, dimension.Value);
            
            var metricsSnapshot = new List<MetricDefinition>(AWS.GetMetrics());
            foreach (var metricDefinition in metricsSnapshot)
            {
                List<double> values;
                lock (metricDefinition.Values)
                {
                    values = new List<double>(metricDefinition.Values);
                }
                targetMembers.Add(metricDefinition.Name, values.Count == 1 ? values[0] : values);
            }
            
            var metadataSnapshot = new Dictionary<string, object>(AWS.CustomMetadata);
            foreach (var metadata in metadataSnapshot) 
                targetMembers.TryAdd(metadata.Key, metadata.Value);

            return targetMembers;
        }
    }

    /// <summary>
    ///     Serializes metrics object to a valid string in JSON format
    /// </summary>
    /// <returns>JSON EMF payload in string format</returns>
    /// <exception cref="SchemaValidationException">namespace</exception>
    public string Serialize()
    {
        if (string.IsNullOrWhiteSpace(AWS.GetNamespace())) throw new SchemaValidationException("namespace");

        // Create a complete snapshot for serialization to avoid concurrent modification issues
        var snapshot = CreateSerializationSnapshot();
        return JsonSerializer.Serialize(snapshot, typeof(RootNode), MetricsSerializationContext.Default);
    }

    /// <summary>
    ///     Creates a complete snapshot of the current state for thread-safe serialization
    /// </summary>
    /// <returns>A snapshot RootNode with all data copied</returns>
    private RootNode CreateSerializationSnapshot()
    {
        var snapshot = new RootNode();
        
        // Copy namespace
        snapshot.AWS.SetNamespace(AWS.GetNamespace());
        
        // Copy service if set
        if (!string.IsNullOrEmpty(AWS.GetService()))
        {
            snapshot.AWS.SetService(AWS.GetService());
        }
        
        // Copy metrics with their values
        var metricsSnapshot = AWS.GetMetrics();
        foreach (var metric in metricsSnapshot)
        {
            List<double> valuesCopy;
            lock (metric.Values)
            {
                valuesCopy = new List<double>(metric.Values);
            }
            
            // Add each value individually to ensure proper metric creation
            foreach (var value in valuesCopy)
            {
                snapshot.AWS.AddMetric(metric.Name, value, metric.Unit, metric.StorageResolution);
            }
        }
        
        // Copy dimensions
        var dimensionsSnapshot = AWS.ExpandAllDimensionSets();
        if (dimensionsSnapshot.Count > 0)
        {
            // Create dimension set with first key-value pair, then add the rest
            var firstKvp = dimensionsSnapshot.First();
            var dimensionSet = new DimensionSet(firstKvp.Key, firstKvp.Value);
            
            // Add remaining dimensions
            foreach (var kvp in dimensionsSnapshot.Skip(1))
            {
                dimensionSet.Dimensions[kvp.Key] = kvp.Value;
            }
            snapshot.AWS.AddDimension(dimensionSet);
        }
        
        // Copy custom metadata
        var metadataSnapshot = new Dictionary<string, object>(AWS.CustomMetadata);
        foreach (var kvp in metadataSnapshot)
        {
            snapshot.AWS.AddMetadata(kvp.Key, kvp.Value);
        }
        
        return snapshot;
    }
}