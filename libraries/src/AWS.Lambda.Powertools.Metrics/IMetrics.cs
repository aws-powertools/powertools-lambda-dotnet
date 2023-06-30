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

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Interface IMetrics
///     Implements the <see cref="System.IDisposable" />
/// </summary>
/// <seealso cref="System.IDisposable" />
public interface IMetrics : IDisposable
{
    /// <summary>
    ///     Adds metric
    /// </summary>
    /// <param name="key">Metric key</param>
    /// <param name="value">Metric value</param>
    /// <param name="unit">Metric unit</param>
    /// <param name="metricResolution"></param>
    void AddMetric(string key, double value, MetricUnit unit, MetricResolution metricResolution);

    /// <summary>
    ///     Adds a dimension
    /// </summary>
    /// <param name="key">Dimension key</param>
    /// <param name="value">Dimension value</param>
    void AddDimension(string key, string value);

    /// <summary>
    ///     Sets the default dimensions
    /// </summary>
    /// <param name="defaultDimension">Default dimensions</param>
    void SetDefaultDimensions(Dictionary<string, string> defaultDimension);

    /// <summary>
    ///     Adds metadata 
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    void AddMetadata(string key, object value);

    /// <summary>
    ///     Pushes a single metric with custom namespace, service and dimensions.
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="value">Metric value</param>
    /// <param name="unit">Metric unit</param>
    /// <param name="nameSpace">Metric namespace</param>
    /// <param name="service">Metric service</param>
    /// <param name="defaultDimensions">Metric default dimensions</param>
    /// <param name="metricResolution">Metrics resolution</param>
    void PushSingleMetric(string metricName, double value, MetricUnit unit, string nameSpace = null,
        string service = null, Dictionary<string, string> defaultDimensions = null, MetricResolution metricResolution = MetricResolution.Default);

    /// <summary>
    ///     Sets the namespace
    /// </summary>
    /// <param name="nameSpace">Metrics namespace</param>
    void SetNamespace(string nameSpace);

    /// <summary>
    ///     Gets the namespace
    /// </summary>
    /// <returns>System.String.</returns>
    string GetNamespace();

    /// <summary>
    ///     Gets the service
    /// </summary>
    /// <returns>System.String.</returns>
    string GetService();

    /// <summary>
    ///     Serializes metrics instance
    /// </summary>
    /// <returns>System.String.</returns>
    string Serialize();

    /// <summary>
    ///     Flushes metrics to CloudWatch
    /// </summary>
    /// <param name="metricsOverflow">if set to <c>true</c> [metrics overflow].</param>
    void Flush(bool metricsOverflow = false);
    
    /// <summary>
    ///     Clears both default dimensions and dimensions lists
    /// </summary>
    void ClearDefaultDimensions();
}
