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
using AWS.Lambda.PowerTools.Logging.Internal;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging
{
    [AttributeUsage(AttributeTargets.Method)]
    public class LoggingAttribute : MethodAspectAttribute
    {
        private LogLevel? _logLevel;
        private double? _samplingRate;
        private bool? _logEvent;
        
        /// <summary>
        /// Service name is used for logging.
        /// This can be also set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// Specify the minimum log level for logging (Information, by default).
        /// This can be also set using the environment variable <c>LOG_LEVEL</c>.
        /// </summary>
        public LogLevel LogLevel
        {
            get => _logLevel ?? LoggingConstants.DefaultLogLevel;
            set => _logLevel = value;
        }
        
        /// <summary>
        /// Dynamically set a percentage of logs to DEBUG level.
        /// This can be also set using the environment variable <c>POWERTOOLS_LOGGER_SAMPLE_RATE</c>.
        /// </summary>
        public double SamplingRate
        {
            get => _samplingRate.GetValueOrDefault();
            set => _samplingRate = value;
        }
        
        /// <summary>
        /// Explicitly log any incoming event, The first handler parameter is the input to the handler,
        /// which can be event data (published by an event source) or custom input that you provide
        /// such as a string or any custom data object.
        /// </summary>
        public bool LogEvent
        {
            get => _logEvent.GetValueOrDefault();
            set => _logEvent = value;
        }
        
        /// <summary>
        /// Pointer path to extract correlation id from input parameter.
        /// The first handler parameter is the input to the handler, which can be
        /// event data (published by an event source) or custom input that you provide
        /// such as a string or any custom data object.
        /// </summary>
        public string CorrelationIdPath { get; set; }

        /// <summary>
        /// Logger is commonly initialized in the global scope.
        /// Due to Lambda Execution Context reuse, this means that custom keys can be persisted across invocations.
        /// Set this attribute to true if you want all custom keys to be deleted on each request.
        /// </summary>
        public bool ClearState { get; set; } = false;
        
        protected override IMethodAspectHandler CreateHandler()
        {
            return new LoggingAspectHandler
            (
                Service,
                _logLevel,
                _samplingRate,
                _logEvent,
                CorrelationIdPath,
                ClearState,
                PowerToolsConfigurations.Instance,
                SystemWrapper.Instance
            );
        }
    }
}