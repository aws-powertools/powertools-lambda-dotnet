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

namespace AWS.Lambda.PowerTools.Metrics
{
    public interface IMetrics : IDisposable
    {   
        void AddMetric(string key, double value, MetricUnit unit);
        void AddDimension(string key, string value);
        void SetDefaultDimensions(Dictionary<string, string> defaultDimension);
        void AddMetadata(string key, dynamic value);
        void PushSingleMetric(string metricName, double value, MetricUnit unit, string nameSpace = null, string service = null, Dictionary<string, string> defaultDimensions = null);
        void SetNamespace(string nameSpace);
        string GetNamespace();
        string GetService();
        string Serialize();
        void Flush(bool metricsOverflow = false);
    }
}
    