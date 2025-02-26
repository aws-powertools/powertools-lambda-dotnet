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
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Interface for metrics operations.
/// </summary>
/// <seealso cref="System.IDisposable" />
public interface IMetrics
{
    /// <summary>
    ///     Adds a metric to the collection.
    /// </summary>
    /// <param name="key">The metric key.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="unit">The metric unit.</param>
    /// <param name="resolution">The metric resolution.</param>
    void AddMetric(string key, double value, MetricUnit unit = MetricUnit.None,
        MetricResolution resolution = MetricResolution.Default);

    /// <summary>
    ///     Adds a dimension to the collection.
    /// </summary>
    /// <param name="key">The dimension key.</param>
    /// <param name="value">The dimension value.</param>
    void AddDimension(string key, string value);

    /// <summary>
    ///     Adds metadata to the collection.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    void AddMetadata(string key, object value);

    /// <summary>
    ///     Sets the default dimensions.
    /// </summary>
    /// <param name="defaultDimensions">The default dimensions.</param>
    void SetDefaultDimensions(Dictionary<string, string> defaultDimensions);

    /// <summary>
    ///     Sets the namespace for the metrics.
    /// </summary>
    /// <param name="nameSpace">The namespace.</param>
    void SetNamespace(string nameSpace);

    /// <summary>
    ///     Sets the service name for the metrics.
    /// </summary>
    /// <param name="service">The service name.</param>
    void SetService(string service);

    /// <summary>
    ///     Sets whether to raise an event on empty metrics.
    /// </summary>
    /// <param name="raiseOnEmptyMetrics">If set to <c>true</c>, raises an event on empty metrics.</param>
    void SetRaiseOnEmptyMetrics(bool raiseOnEmptyMetrics);

    /// <summary>
    ///     Sets whether to capture cold start metrics.
    /// </summary>
    /// <param name="captureColdStart">If set to <c>true</c>, captures cold start metrics.</param>
    void SetCaptureColdStart(bool captureColdStart);

    /// <summary>
    ///     Pushes a single metric to the collection.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="unit">The metric unit.</param>
    /// <param name="nameSpace">The namespace.</param>
    /// <param name="service">The service name.</param>
    /// <param name="defaultDimensions">The default dimensions.</param>
    /// <param name="resolution">The metric resolution.</param>
    void PushSingleMetric(string name, double value, MetricUnit unit, string nameSpace = null, string service = null,
        Dictionary<string, string> defaultDimensions = null, MetricResolution resolution = MetricResolution.Default);

    /// <summary>
    ///     Clears the default dimensions.
    /// </summary>
    void ClearDefaultDimensions();

    /// <summary>
    ///     Flushes the metrics.
    /// </summary>
    /// <param name="metricsOverflow">If set to <c>true</c>, indicates a metrics overflow.</param>
    void Flush(bool metricsOverflow = false);

    /// <summary>
    ///     Gets the metrics options.
    /// </summary>
    /// <value>The metrics options.</value>
    public MetricsOptions Options { get; }
    
    /// <summary>
    ///    Captures the cold start metric.
    /// </summary>
    /// <param name="context"></param>
    void CaptureColdStartMetric(ILambdaContext context);
}