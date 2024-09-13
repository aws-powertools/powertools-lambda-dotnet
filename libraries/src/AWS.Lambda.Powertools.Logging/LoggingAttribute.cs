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
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Provides a Lambda optimized logger with output structured as JSON.                            <br/>
///                                                                                                   <br/>
///     Key features                                                                                  <br/>
///     ---------------------                                                                         <br/> 
///     <list type="bullet">
///         <item>
///             <description>Capture key fields from Lambda context and cold start</description>
///         </item>
///         <item>
///             <description>Log Lambda event when instructed (disabled by default)</description>
///         </item>
///         <item>
///             <description>Log sampling enables DEBUG log level for a percentage of requests (disabled by default)</description>
///         </item>
///         <item>
///             <description>Append additional keys to structured log at any point in time</description>
///         </item>
///     </list>
///                                                                                                   <br/> 
///     Environment variables                                                                         <br/>
///     ---------------------                                                                         <br/> 
///     <list type="table">
///         <listheader>
///           <term>Variable name</term>
///           <description>Description</description>
///         </listheader>
///         <item>
///             <term>POWERTOOLS_SERVICE_NAME</term>
///             <description>string, service name</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_LOG_LEVEL</term>
///             <description>string, logging level (e.g. Information, Debug, and Trace)</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_LOGGER_CASE</term>
///             <description>string, logger output case (e.g. CamelCase, PascalCase, and SnakeCase)</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_LOGGER_SAMPLE_RATE</term>
///             <description>double, sampling rate ranging from 0 to 1, 1 being 100% sampling</description>
///         </item>
///     </list>
///                                                                                                   <br/>
///     Parameters                                                                                    <br/>
///     -----------                                                                                   <br/>
///     <list type="table">
///         <listheader>
///           <term>Parameter name</term>
///           <description>Description</description>
///         </listheader>
///         <item>
///             <term>Service</term>
///             <description>string, service name to be appended in logs, by default "service_undefined"</description>
///         </item>
///         <item>
///             <term>LogLevel</term>
///             <description>enum, logging level (e.g. Information, Debug, and Trace), by default Information</description>
///         </item>
///         <item>
///             <term>LoggerOutputCase</term>
///             <description>enum, logger output case (e.g. CamelCase, PascalCase, and SnakeCase)</description>
///         </item>
///         <item>
///             <term>SamplingRate</term>
///             <description>double, sample rate for debug calls within execution context defaults to 0.0</description>
///         </item>
///         <item>
///             <term>CorrelationIdPath</term>
///             <description>string, pointer path to extract correlation id from input parameter</description>
///         </item>
///         <item>
///             <term>ClearState</term>
///             <description>bool, clear all custom keys on each request, by default false</description>
///         </item>
///     </list>
/// </summary>
/// <example>
///     <code>
///         [Logging(
///             Service = "Example",
///             LogEvent = true,
///             ClearState = true,
///             LogLevel = LogLevel.Debug,
///             LoggerOutputCase = LoggerOutputCase.SnakeCase,
///             CorrelationIdPath = "/headers/my_request_id_header")
///         ]
///         public async Task&lt;APIGatewayProxyResponse&gt; FunctionHandler
///              (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
///         {
///             ...
///         }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
[Injection(typeof(LoggingAspect))]
public class LoggingAttribute : Attribute
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
    public double SamplingRate { get; set; }

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
    public LoggerOutputCase LoggerOutputCase  { get; set; } = LoggerOutputCase.Default;

    // /// <summary>
    // ///     Creates the handler.
    // /// </summary>
    // /// <returns>IMethodAspectHandler.</returns>
    // protected override IMethodAspectHandler CreateHandler()
    // {
    //     var config = new LoggerConfiguration 
    //     {
    //         Service = Service,
    //         LoggerOutputCase = LoggerOutputCase,
    //         SamplingRate = SamplingRate,
    //     };
    //     
    //     return new LoggingAspect
    //     (
    //         config,
    //         LogLevel,
    //         LogEvent,
    //         CorrelationIdPath,
    //         ClearState,
    //         PowertoolsConfigurations.Instance,
    //         SystemWrapper.Instance
    //     );
    // }
}