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

public class MetricsBuilder
{
    private readonly MetricsOptions _options = new();
    
    public MetricsBuilder WithNamespace(string nameSpace)
    {
        _options.Namespace = nameSpace;
        return this;
    }
    
    public MetricsBuilder WithService(string service)
    {
        _options.Service = service;
        return this;
    }
    
    public MetricsBuilder WithRaiseOnEmptyMetrics(bool raiseOnEmptyMetrics)
    {
        _options.RaiseOnEmptyMetrics = raiseOnEmptyMetrics;
        return this;
    }
    
    public MetricsBuilder WithCaptureColdStart(bool captureColdStart)
    {
        _options.CaptureColdStart = captureColdStart;
        return this;
    }
    
    public MetricsBuilder WithDefaultDimensions(Dictionary<string, string> defaultDimensions)
    {
        _options.DefaultDimensions = defaultDimensions;
        return this;
    }
    
    public IMetrics Build()
    {
        return Metrics.Configure(opt =>
        {
            opt.Namespace = _options.Namespace;
            opt.Service = _options.Service;
            opt.RaiseOnEmptyMetrics = _options.RaiseOnEmptyMetrics;
            opt.CaptureColdStart = _options.CaptureColdStart;
            opt.DefaultDimensions = _options.DefaultDimensions;
        });
    }
}