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
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Class MetricDefinition.
/// </summary>
public class MetricDefinition
{
    /// <summary>
    ///     Creates a MetricDefinition object. MetricUnit is set to NONE since it is not provided.
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="value">Metric value</param>
    public MetricDefinition(string name, double value) : this(name, MetricUnit.None, new List<double> {value})
    {
    }

    /// <summary>
    ///     Creates a MetricDefinition object
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="unit">Metric unit</param>
    /// <param name="value">Metric value</param>
    public MetricDefinition(string name, MetricUnit unit, double value) : this(name, unit, new List<double> {value})
    {
    }

    /// <summary>
    ///     Creates a MetricDefinition object with multiple values
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="unit">Metric unit</param>
    /// <param name="values">List of metric values</param>
    public MetricDefinition(string name, MetricUnit unit, List<double> values)
    {
        Name = name;
        Unit = unit;
        Values = values;
    }

    /// <summary>
    ///     Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    [JsonPropertyName(nameof(Name))]
    public string Name { get; set; }

    /// <summary>
    ///     Gets the values.
    /// </summary>
    /// <value>The values.</value>
    [JsonIgnore]
    public List<double> Values { get; }

    /// <summary>
    ///     Gets or sets the unit.
    /// </summary>
    /// <value>The unit.</value>
    [JsonPropertyName(nameof(Unit))]
    public MetricUnit Unit { get; set; }

    /// <summary>
    ///     Adds value to existing metric with same key
    /// </summary>
    /// <param name="value">Metric value to add to existing key</param>
    public void AddValue(double value)
    {
        Values.Add(value);
    }
}