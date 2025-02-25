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

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
/// Provides a builder for configuring metrics.
/// </summary>
public class MetricsBuilder
{
    private readonly MetricsOptions _options = new();
    
    /// <summary>
    /// Sets the namespace for the metrics.
    /// </summary>
    /// <param name="nameSpace">The namespace identifier.</param>
    /// <returns>The current instance of <see cref="MetricsBuilder"/>.</returns>
    public MetricsBuilder WithNamespace(string nameSpace)
    {
        _options.Namespace = nameSpace;
        return this;
    }
    
    /// <summary>
    /// Sets the service name for the metrics.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <returns>The current instance of <see cref="MetricsBuilder"/>.</returns>
    public MetricsBuilder WithService(string service)
    {
        _options.Service = service;
        return this;
    }
    
    /// <summary>
    /// Sets whether to raise an exception if no metrics are captured.
    /// </summary>
    /// <param name="raiseOnEmptyMetrics">If true, raises an exception when no metrics are captured.</param>
    /// <returns>The current instance of <see cref="MetricsBuilder"/>.</returns>
    public MetricsBuilder WithRaiseOnEmptyMetrics(bool raiseOnEmptyMetrics)
    {
        _options.RaiseOnEmptyMetrics = raiseOnEmptyMetrics;
        return this;
    }
    
    /// <summary>
    /// Sets whether to capture cold start metrics.
    /// </summary>
    /// <param name="captureColdStart">If true, captures cold start metrics.</param>
    /// <returns>The current instance of <see cref="MetricsBuilder"/>.</returns>
    public MetricsBuilder WithCaptureColdStart(bool captureColdStart)
    {
        _options.CaptureColdStart = captureColdStart;
        return this;
    }
    
    /// <summary>
    /// Sets the default dimensions for the metrics.
    /// </summary>
    /// <param name="defaultDimensions">A dictionary of default dimensions.</param>
    /// <returns>The current instance of <see cref="MetricsBuilder"/>.</returns>
    public MetricsBuilder WithDefaultDimensions(Dictionary<string, string> defaultDimensions)
    {
        _options.DefaultDimensions = defaultDimensions;
        return this;
    }
    
    /// <summary>
    /// Sets the function name for the metrics dimension.
    /// </summary>
    /// <param name="functionName"></param>
    /// <returns></returns>
    public MetricsBuilder WithFunctionName(string functionName)
    {
        _options.FunctionName = functionName;
        return this;
    }
    
    /// <summary>
    /// Builds and configures the metrics instance.
    /// </summary>
    /// <returns>An instance of <see cref="IMetrics"/>.</returns>
    public IMetrics Build()
    {
        return Metrics.Configure(opt =>
        {
            opt.Namespace = _options.Namespace;
            opt.Service = _options.Service;
            opt.RaiseOnEmptyMetrics = _options.RaiseOnEmptyMetrics;
            opt.CaptureColdStart = _options.CaptureColdStart;
            opt.DefaultDimensions = _options.DefaultDimensions;
            opt.FunctionName = _options.FunctionName;
        });
    }
}