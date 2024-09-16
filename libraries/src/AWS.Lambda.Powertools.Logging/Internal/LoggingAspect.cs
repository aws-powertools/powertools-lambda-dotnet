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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Serializers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Logging Aspect
///     Scope.Global is singleton
/// </summary>
/// <seealso cref="IMethodAspectHandler" />
[Aspect(Scope.Global, Factory = typeof(LoggingAspectFactory))]
public class LoggingAspect
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
    private bool _clearState;

    /// <summary>
    ///     The correlation identifier path
    /// </summary>
    private string _correlationIdPath;

    /// <summary>
    ///     The log event
    /// </summary>
    private bool? _logEvent;

    /// <summary>
    ///     The log level
    /// </summary>
    private LogLevel? _logLevel;

    /// <summary>
    ///     The Powertools for AWS Lambda (.NET) configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

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
    ///     The configuration
    /// </summary>
    private LoggerConfiguration _config;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggingAspect" /> class.
    /// </summary>
    /// <param name="powertoolsConfigurations">The Powertools configurations.</param>
    /// <param name="systemWrapper">The system wrapper.</param>
    public LoggingAspect(IPowertoolsConfigurations powertoolsConfigurations, ISystemWrapper systemWrapper)
    {
        _powertoolsConfigurations = powertoolsConfigurations;
        _systemWrapper = systemWrapper;
    }

    /// <summary>
    /// Runs before the execution of the method marked with the Logging Attribute
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <param name="hostType"></param>
    /// <param name="method"></param>
    /// <param name="returnType"></param>
    /// <param name="triggers"></param>
    [Advice(Kind.Before)]
    public void OnEntry(
        [Argument(Source.Instance)] object instance,
        [Argument(Source.Name)] string name,
        [Argument(Source.Arguments)] object[] args,
        [Argument(Source.Type)] Type hostType,
        [Argument(Source.Metadata)] MethodBase method,
        [Argument(Source.ReturnType)] Type returnType,
        [Argument(Source.Triggers)] Attribute[] triggers)
    {
        // Called before the method
        var trigger = triggers.OfType<LoggingAttribute>().First();

        try
        {
            var eventArgs = new AspectEventArgs
            {
                Instance = instance,
                Type = hostType,
                Method = method,
                Name = name,
                Args = args,
                ReturnType = returnType,
                Triggers = triggers
            };

            _config = new LoggerConfiguration
            {
                Service = trigger.Service,
                LoggerOutputCase = trigger.LoggerOutputCase,
                SamplingRate = trigger.SamplingRate
            };

            _logLevel = trigger.LogLevel;
            _logEvent = trigger.LogEvent;
            _correlationIdPath = trigger.CorrelationIdPath;
            _clearState = trigger.ClearState;

            Logger.LoggerProvider ??= new LoggerProvider(_config, _powertoolsConfigurations, _systemWrapper);

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
        catch (Exception exception)
        {
            // The purpose of ExceptionDispatchInfo.Capture is to capture a potentially mutating exception's StackTrace at a point in time:
            // https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions#capture-exceptions-to-rethrow-later
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    /// <summary>
    ///     Handles the Kind.After event.
    /// </summary>
    [Advice(Kind.After)]
    public void OnExit()
    {
        if (!_isContextInitialized)
            return;
        if (_clearLambdaContext)
            LoggingLambdaContext.Clear();
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
        _clearLambdaContext = LoggingLambdaContext.Extract(eventArgs);
        if (LoggingLambdaContext.Instance is null && IsDebug())
            _systemWrapper.LogLine(
                "Skipping Lambda Context injection because ILambdaContext context parameter not found.");
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
            var jsonDoc =
                JsonDocument.Parse(PowertoolsLoggingSerializer.Serialize(eventArg, eventArg.GetType()));
#else
            var jsonDoc = JsonDocument.Parse(PowertoolsLoggingSerializer.Serialize(eventArg));
#endif
            var element = jsonDoc.RootElement;

            for (var i = 0; i < correlationIdPaths.Length; i++)
            {
                // For casing parsing to be removed from Logging v2 when we get rid of outputcase
                var pathWithOutputCase =
                    _powertoolsConfigurations.ConvertToOutputCase(correlationIdPaths[i], _config.LoggerOutputCase);
                if (!element.TryGetProperty(pathWithOutputCase, out var childElement))
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
        LoggingLambdaContext.Clear();
        Logger.LoggerProvider = null;
        Logger.RemoveAllKeys();
    }
}