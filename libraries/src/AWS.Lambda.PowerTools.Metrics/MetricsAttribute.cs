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
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;
using AWS.Lambda.PowerTools.Metrics.Internal;

namespace AWS.Lambda.PowerTools.Metrics
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MetricsAttribute : MethodAspectAttribute
    {
        /// <summary>
        /// Set namespace to current subsegment.
        /// The default is the environment variable <c>POWERTOOLS_METRICS_NAMESPACE</c>.
        /// </summary>
        public string Namespace { get; set; }
        
        /// <summary>
        /// Service name is used for metric dimension across all metrics.
        /// This can be also set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
        /// </summary>
        public string Service { get; set; }
        
        /// <summary>
        /// Captures cold start during Lambda execution
        /// </summary>
        public bool CaptureColdStart { get; set; }
        
        /// <summary>
        /// Instructs metrics validation to throw exception if no metrics are provided.
        /// </summary>
        public bool RaiseOnEmptyMetrics { get; set; }
        
        
        private IMetrics _metricsInstance;
        private IMetrics MetricsInstance =>
            _metricsInstance ??= new Metrics(
                PowerToolsConfigurations.Instance,
                Namespace,
                Service,
                RaiseOnEmptyMetrics
            );
        
        protected override IMethodAspectHandler CreateHandler()
        {
            return new MetricsAspectHandler
            (
                MetricsInstance,
                CaptureColdStart
            );
        }
    }
}



