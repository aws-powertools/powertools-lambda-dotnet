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
using System.Linq;
using Amazon.Lambda.Core;
using AWS.Lambda.PowerTools.Aspects;

namespace AWS.Lambda.PowerTools.Metrics.Internal
{
    internal class MetricsAspectHandler : IMethodAspectHandler
    {
        private readonly IMetrics _metrics;
        private readonly bool _captureColdStartEnabled;
        private static bool _isColdStart = true;

        /// <summary>
        /// Creates MetricsAspectHandler to supports running code before and after marked methods.
        /// </summary>
        /// <param name="metricsInstance">AWS.Lambda.PowerTools.Metrics Instance</param>
        /// <param name="captureColdStartEnabled">If 'true', captures cold start during Lambda execution</param>
        internal MetricsAspectHandler
        (
            IMetrics metricsInstance,
            bool captureColdStartEnabled
        )
        {
            _metrics = metricsInstance;
            _captureColdStartEnabled = captureColdStartEnabled;
        }

        /// <summary>
        /// OnExit runs after the execution of the method marked with the Metrics Attribute
        /// </summary>
        /// <param name="eventArgs">Aspect Arguments</param>
        public void OnExit(AspectEventArgs eventArgs)
        {            
            _metrics.Flush();
        }

        /// <summary>
        /// OnEntry runs before the execution of the method marked with the Metrics Attribute
        /// </summary>
        /// <param name="eventArgs">Aspect Arguments</param>
        public void OnEntry(AspectEventArgs eventArgs)
        {
            if (!_isColdStart || !_captureColdStartEnabled) return;

            var nameSpace = _metrics.GetNamespace();
            var service = _metrics.GetService();
            Dictionary<string, string> dimensions = null;

            if (eventArgs.Args?.FirstOrDefault(x => x is ILambdaContext) is ILambdaContext context)
            {
                dimensions = new Dictionary<string, string>
                {
                    {"FunctionName", context.FunctionName}
                };
            }

            _metrics.PushSingleMetric(
                "ColdStart",
                1.0,
                MetricUnit.COUNT,
                nameSpace,
                service,
                dimensions
            );
            
            _isColdStart = false;
        }

        /// <summary>
        /// OnSuccess run after successful execution of the method marked with the Metrics Attribute
        /// </summary>
        /// <param name="eventArgs">Aspect Arguments</param>
        /// <param name="result">Object returned by the method marked with Metrics Attribute</param>
        public void OnSuccess(AspectEventArgs eventArgs, object result) { }

        /// <summary>
        /// OnException runs when an unhandled exception occurs inside the method marked with the Metrics Attribute
        /// </summary>
        /// <param name="eventArgs">Aspect Arguments</param>
        /// <param name="exception">Exception thrown by the method marked with Metrics Attribute</param>
        /// <typeparam name="T">Type of Exception expected</typeparam>
        /// <returns>Type of the Exception expected</returns>
        /// <exception cref="Exception">Generic unhandled exception</exception>
        public T OnException<T>(AspectEventArgs eventArgs, Exception exception)
        {
            throw exception;
        }
        
        /// <summary>
        /// Helper method for testing purposes. Clears static instance between test execution
        /// </summary>
        internal void ResetForTest()
        {
            _isColdStart = true;
            Metrics.ResetForTest();
        }
    }
}