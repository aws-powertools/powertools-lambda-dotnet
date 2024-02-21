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
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.ApplicationLoadBalancerEvents;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Converters;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;


/// <summary>
///     Class LoggingAspectHandler.
///     Implements the <see cref="IMethodAspectHandler" />
/// </summary>
/// <seealso cref="IMethodAspectHandler" />
internal class LoggingAspectHandler : IMethodAspectHandler
{
    /// <summary>
    ///     The is cold start
    /// </summary>
    private static bool _isColdStart = true;

    /// <summary>
    ///     The initialize context
    /// </summary>
    private static bool _initializeContext = true;

    /// <summary>
    ///     Clear state?
    /// </summary>
    private readonly bool _clearState;

    /// <summary>
    ///     The correlation identifier path
    /// </summary>
    private readonly string _correlationIdPath;

    /// <summary>
    ///     The log event
    /// </summary>
    private readonly bool? _logEvent;

    /// <summary>
    ///     The log level
    /// </summary>
    private readonly LogLevel? _logLevel;
    
    /// <summary>
    ///     The logger output case
    /// </summary>
    private readonly LoggerOutputCase? _loggerOutputCase;

    /// <summary>
    ///     The Powertools for AWS Lambda (.NET) configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     The sampling rate
    /// </summary>
    private readonly double? _samplingRate;

    /// <summary>
    ///     Service name
    /// </summary>
    private readonly string _service;

    /// <summary>
    ///     The system wrapper
    /// </summary>
    private readonly ISystemWrapper _systemWrapper;

    /// <summary>
    ///     The is context initialized
    /// </summary>
    private bool _isContextInitialized;
    
    /// <summary>
    ///     Specify to clear Lambda Context on exit
    /// </summary>
    private bool _clearLambdaContext;
    
    /// <summary>
    ///     The JsonSerializer options
    /// </summary>
    private static JsonSerializerOptions _jsonSerializerOptions;
    
    /// <summary>
    ///     Get JsonSerializer options.
    /// </summary>
    /// <value>The current configuration.</value>
    private static JsonSerializerOptions JsonSerializerOptions =>
        _jsonSerializerOptions ??= BuildJsonSerializerOptions();

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggingAspectHandler" /> class.
    /// </summary>
    /// <param name="service">Service name</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="loggerOutputCase">The logger output case.</param>
    /// <param name="samplingRate">The sampling rate.</param>
    /// <param name="logEvent">if set to <c>true</c> [log event].</param>
    /// <param name="correlationIdPath">The correlation identifier path.</param>
    /// <param name="clearState">if set to <c>true</c> [clear state].</param>
    /// <param name="powertoolsConfigurations">The Powertools configurations.</param>
    /// <param name="systemWrapper">The system wrapper.</param>
    internal LoggingAspectHandler
    (
        string service,
        LogLevel? logLevel,
        LoggerOutputCase? loggerOutputCase,
        double? samplingRate,
        bool? logEvent,
        string correlationIdPath,
        bool clearState,
        IPowertoolsConfigurations powertoolsConfigurations,
        ISystemWrapper systemWrapper
    )
    {
        _service = service;
        _logLevel = logLevel;
        _loggerOutputCase = loggerOutputCase;
        _samplingRate = samplingRate;
        _logEvent = logEvent;
        _clearState = clearState;
        _correlationIdPath = correlationIdPath;
        _powertoolsConfigurations = powertoolsConfigurations;
        _systemWrapper = systemWrapper;
    }

    /// <summary>
    ///     Handles the <see cref="E:Entry" /> event.
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    public void OnEntry(AspectEventArgs eventArgs)
    {
        var loggerConfig = new LoggerConfiguration
        {
            Service = _service,
            MinimumLevel = _logLevel,
            SamplingRate = _samplingRate,
            LoggerOutputCase = _loggerOutputCase
        };

        switch (Logger.LoggerProvider)
        {
            case null:
                Logger.LoggerProvider = new LoggerProvider(loggerConfig);
                break;
            case LoggerProvider:
                ((LoggerProvider) Logger.LoggerProvider).Configure(loggerConfig);
                break;
        }

        if (!_initializeContext)
            return;

        Logger.AppendKey(LoggingConstants.KeyColdStart, _isColdStart);

        _isColdStart = false;
        _initializeContext = false;
        _isContextInitialized = true;

        var eventObject = eventArgs.Args.FirstOrDefault();
        CaptureXrayTraceId();
        CaptureLambdaContext(eventArgs);
        CaptureCorrelationId(eventObject);
        if (_logEvent ?? _powertoolsConfigurations.LoggerLogEvent)
            LogEvent(eventObject);
    }

    /// <summary>
    ///     Called when [success].
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    /// <param name="result">The result.</param>
    public void OnSuccess(AspectEventArgs eventArgs, object result)
    {
    }

    /// <summary>
    ///     Called when [exception].
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    /// <param name="exception">The exception.</param>
    public void OnException(AspectEventArgs eventArgs, Exception exception)
    {
        // The purpose of ExceptionDispatchInfo.Capture is to capture a potentially mutating exception's StackTrace at a point in time:
        // https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions#capture-exceptions-to-rethrow-later
        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    /// <summary>
    ///     Handles the <see cref="E:Exit" /> event.
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    public void OnExit(AspectEventArgs eventArgs)
    {
        if (!_isContextInitialized)
            return;
        if (_clearLambdaContext)
            PowertoolsLambdaContext.Clear();
        if (_clearState)
            Logger.RemoveAllKeys();
        _initializeContext = true;
    }

    /// <summary>
    ///     Determines whether this instance is debug.
    /// </summary>
    /// <returns><c>true</c> if this instance is debug; otherwise, <c>false</c>.</returns>
    private bool IsDebug()
    {
        return LogLevel.Debug >= _powertoolsConfigurations.GetLogLevel(_logLevel);
    }

    /// <summary>
    ///     Captures the xray trace identifier.
    /// </summary>
    private void CaptureXrayTraceId()
    {
        var xRayTraceId = _powertoolsConfigurations.XRayTraceId;
        if (string.IsNullOrWhiteSpace(xRayTraceId))
            return;

        xRayTraceId = xRayTraceId
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .First()
            .Replace("Root=", "");

        Logger.AppendKey(LoggingConstants.KeyXRayTraceId, xRayTraceId);
    }

    /// <summary>
    ///     Captures the lambda context.
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    private void CaptureLambdaContext(AspectEventArgs eventArgs)
    {
        _clearLambdaContext = PowertoolsLambdaContext.Extract(eventArgs);
        if (PowertoolsLambdaContext.Instance is null && IsDebug())
            _systemWrapper.LogLine(
                "Skipping Lambda Context injection because ILambdaContext context parameter not found.");
    }
    
    /// <summary>
    ///     Builds JsonSerializer options.
    /// </summary>
    private static JsonSerializerOptions BuildJsonSerializerOptions()
    {
        var jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#if NET8_0_OR_GREATER
            TypeInfoResolver = PowertoolsLoggerSourceGenerationContext.Default
#endif
        };
        jsonOptions.Converters.Add(new ByteArrayConverter());
        jsonOptions.Converters.Add(new ExceptionConverter());
        jsonOptions.Converters.Add(new MemoryStreamConverter());
        jsonOptions.Converters.Add(new ConstantClassConverter());
        jsonOptions.Converters.Add(new DateOnlyConverter());
        jsonOptions.Converters.Add(new TimeOnlyConverter());
        return jsonOptions;
    }

    /// <summary>
    ///     Captures the correlation identifier.
    /// </summary>
    /// <param name="eventArg">The event argument.</param>
    private void CaptureCorrelationId(object eventArg)
    {
        if (string.IsNullOrWhiteSpace(_correlationIdPath))
            return;

        var correlationIdPaths = _correlationIdPath
            .Split(CorrelationIdPaths.Separator, StringSplitOptions.RemoveEmptyEntries);

        if (!correlationIdPaths.Any())
            return;

        if (eventArg is null)
        {
            if (IsDebug())
                _systemWrapper.LogLine(
                    "Skipping CorrelationId capture because event parameter not found.");
            return;
        }

        try
        {
            var correlationId = string.Empty;
            
#if NET8_0_OR_GREATER
            var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(eventArg,eventArg.GetType(), JsonSerializerOptions));
#else
            var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(eventArg, JsonSerializerOptions));
#endif
            var element = jsonDoc.RootElement;

            for (var i = 0; i < correlationIdPaths.Length; i++)
            {
                if (!element.TryGetProperty(correlationIdPaths[i], out var childElement))
                    break;

                element = childElement;
                if (i == correlationIdPaths.Length - 1)
                    correlationId = element.ToString();
            }

            if (!string.IsNullOrWhiteSpace(correlationId))
                Logger.AppendKey(LoggingConstants.KeyCorrelationId, correlationId);
        }
        catch (Exception e)
        {
            if (IsDebug())
                _systemWrapper.LogLine(
                    $"Skipping CorrelationId capture because of error caused while parsing the event object {e.Message}.");
        }
    }

    /// <summary>
    ///     Logs the event.
    /// </summary>
    /// <param name="eventArg">The event argument.</param>
    private void LogEvent(object eventArg)
    {
        switch (eventArg)
        {
            case null:
            {
                if (IsDebug())
                    _systemWrapper.LogLine(
                        "Skipping Event Log because event parameter not found.");
                break;
            }
            case Stream:
                try
                {
                    Logger.LogInformation(eventArg);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to log event from supplied input stream.");
                }

                break;
            default:
                try
                {
                    Logger.LogInformation(eventArg);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to log event from supplied input object.");
                }

                break;
        }
    }

    /// <summary>
    ///     Resets for test.
    /// </summary>
    internal static void ResetForTest()
    {
        _isColdStart = true;
        _initializeContext = true;
        PowertoolsLambdaContext.Clear();
        Logger.LoggerProvider = null;
        Logger.RemoveAllKeys();
    }
}

#if NET8_0_OR_GREATER
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ApplicationLoadBalancerRequest))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
public partial class PowertoolsLoggerSourceGenerationContext : PowertoolsSourceGenerationContext
{
    // make public
}
#endif