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

namespace AWS.Lambda.PowerTools.Metrics;

/// <summary>
///     Interface IMetrics
///     Implements the <see cref="System.IDisposable" />
/// </summary>
/// <seealso cref="System.IDisposable" />
public interface IMetrics : IDisposable
{
    /// <summary>
    ///     Adds the metric.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="unit">The unit.</param>
    void AddMetric(string key, double value, MetricUnit unit);

    /// <summary>
    ///     Adds the dimension.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void AddDimension(string key, string value);

    /// <summary>
    ///     Sets the default dimensions.
    /// </summary>
    /// <param name="defaultDimension">The default dimension.</param>
    void SetDefaultDimensions(Dictionary<string, string> defaultDimension);

    /// <summary>
    ///     Adds the metadata.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void AddMetadata(string key, dynamic value);

    /// <summary>
    ///     Pushes the single metric.
    /// </summary>
    /// <param name="metricName">Name of the metric.</param>
    /// <param name="value">The value.</param>
    /// <param name="unit">The unit.</param>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="service">The service.</param>
    /// <param name="defaultDimensions">The default dimensions.</param>
    void PushSingleMetric(string metricName, double value, MetricUnit unit, string nameSpace = null,
        string service = null, Dictionary<string, string> defaultDimensions = null);

    /// <summary>
    ///     Sets the namespace.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    void SetNamespace(string nameSpace);

    /// <summary>
    ///     Gets the namespace.
    /// </summary>
    /// <returns>System.String.</returns>
    string GetNamespace();

    /// <summary>
    ///     Gets the service.
    /// </summary>
    /// <returns>System.String.</returns>
    string GetService();

    /// <summary>
    ///     Serializes this instance.
    /// </summary>
    /// <returns>System.String.</returns>
    string Serialize();

    /// <summary>
    ///     Flushes the specified metrics overflow.
    /// </summary>
    /// <param name="metricsOverflow">if set to <c>true</c> [metrics overflow].</param>
    void Flush(bool metricsOverflow = false);
}