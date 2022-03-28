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
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Creates and setups a logger to format statements in JSON.
/// 
///     Includes service name and any additional key=value into logs
///     It also accepts both service name or level explicitly via env vars
///
///     Environment variables
///     ---------------------
///     POWERTOOLS_SERVICE_NAME : string
///         service name
///     POWERTOOLS_LOG_LEVEL: string
///         logging level (e.g. Information, Debug, and Trace)
///     POWERTOOLS_LOGGER_CASE: string
///         logger output case (e.g. CamelCase, PascalCase, and SnakeCase)
///     POWERTOOLS_LOGGER_SAMPLE_RATE: double
///         sampling rate ranging from 0 to 1, 1 being 100% sampling
///
///     Parameters
///     ----------
///     Service : string, optional
///         service name to be appended in logs, by default "service_undefined"
///     LogLevel : enum, optional
///         logging level (e.g. Information, Debug, and Trace), by default Information
///     LoggerOutputCase : enum, optional
///         logger output case (e.g. CamelCase, PascalCase, and SnakeCase)
///     SamplingRate: double, optional
///         sample rate for debug calls within execution context defaults to 0.0
///     CorrelationIdPath: string, optional
///         pointer path to extract correlation id from input parameter.
///     ClearState: bool, optional
///         clear all custom keys on each request, by default false
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class LoggingAttribute : MethodAspectAttribute
{
    /// <summary>
    ///     The log event
    /// </summary>
    private bool? _logEvent;

    /// <summary>
    ///     The log level
    /// </summary>
    private LogLevel? _logLevel;
    
    /// <summary>
    ///     The logger output case
    /// </summary>
    private LoggerOutputCase? _loggerOutputCase;

    /// <summary>
    ///     The sampling rate
    /// </summary>
    private double? _samplingRate;

    /// <summary>
    ///     Service name is used for logging.
    ///     This can be also set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
    /// </summary>
    /// <value>The service.</value>
    public string Service { get; set; }

    /// <summary>
    ///     Specify the minimum log level for logging (Information, by default).
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOG_LEVEL</c>.
    /// </summary>
    /// <value>The log level.</value>
    public LogLevel LogLevel
    {
        get => _logLevel ?? LoggingConstants.DefaultLogLevel;
        set => _logLevel = value;
    }

    /// <summary>
    ///     Dynamically set a percentage of logs to DEBUG level.
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOGGER_SAMPLE_RATE</c>.
    /// </summary>
    /// <value>The sampling rate.</value>
    public double SamplingRate
    {
        get => _samplingRate.GetValueOrDefault();
        set => _samplingRate = value;
    }

    /// <summary>
    ///     Explicitly log any incoming event, The first handler parameter is the input to the handler,
    ///     which can be event data (published by an event source) or custom input that you provide
    ///     such as a string or any custom data object.
    /// </summary>
    /// <value><c>true</c> if [log event]; otherwise, <c>false</c>.</value>
    public bool LogEvent
    {
        get => _logEvent.GetValueOrDefault();
        set => _logEvent = value;
    }

    /// <summary>
    ///     Pointer path to extract correlation id from input parameter.
    ///     The first handler parameter is the input to the handler, which can be
    ///     event data (published by an event source) or custom input that you provide
    ///     such as a string or any custom data object.
    /// </summary>
    /// <value>The correlation identifier path.</value>
    public string CorrelationIdPath { get; set; }

    /// <summary>
    ///     Logger is commonly initialized in the global scope.
    ///     Due to Lambda Execution Context reuse, this means that custom keys can be persisted across invocations.
    ///     Set this attribute to true if you want all custom keys to be deleted on each request.
    /// </summary>
    /// <value><c>true</c> if [clear state]; otherwise, <c>false</c>.</value>
    public bool ClearState { get; set; } = false;
    
    /// <summary>
    ///     Specify output case for logging (SnakeCase, by default).
    ///     This can be also set using the environment variable <c>POWERTOOLS_LOGGER_CASE</c>.
    /// </summary>
    /// <value>The log level.</value>
    public LoggerOutputCase LoggerOutputCase
    {
        get => _loggerOutputCase ?? LoggingConstants.DefaultLoggerOutputCase;
        set => _loggerOutputCase = value;
    }

    /// <summary>
    ///     Creates the handler.
    /// </summary>
    /// <returns>IMethodAspectHandler.</returns>
    protected override IMethodAspectHandler CreateHandler()
    {
        return new LoggingAspectHandler
        (
            Service,
            _logLevel,
            _loggerOutputCase,
            _samplingRate,
            _logEvent,
            CorrelationIdPath,
            ClearState,
            PowertoolsConfigurations.Instance,
            SystemWrapper.Instance
        );
    }
}