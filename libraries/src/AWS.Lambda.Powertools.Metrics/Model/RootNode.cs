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

            foreach (var dimension in AWS.ExpandAllDimensionSets()) targetMembers.Add(dimension.Key, dimension.Value);

            foreach (var metadata in AWS.CustomMetadata) targetMembers.Add(metadata.Key, metadata.Value);

            foreach (var metricDefinition in AWS.GetMetrics())
            {
                var values = metricDefinition.Values;
                targetMembers.Add(metricDefinition.Name, values.Count == 1 ? values[0] : values);
            }

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

        return JsonSerializer.Serialize(this);
    }
}