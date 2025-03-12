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
    private bool _isColdStart = true;

    /// <summary>
    ///     The initialize context
    /// </summary>
    private bool _initializeContext = true;

    /// <summary>
    ///     Clear state?
    /// </summary>
    private bool _clearState;

    /// <summary>
    ///     The correlation identifier path
    /// </summary>
    private string _correlationIdPath;
    
    /// <summary>
    ///     Paths to arbitrary values to extract from the input object and the names of the keys to store them in 
    /// </summary>
    private IEnumerable<(string Path, string KeyName)> _extractedKeyPaths;

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
                SamplingRate = trigger.SamplingRate,
                MinimumLevel = trigger.LogLevel
            };

            var logEvent = trigger.LogEvent;
            _correlationIdPath = trigger.CorrelationIdPath;
            _extractedKeyPaths = trigger.ExtractedKeyPaths;
            _clearState = trigger.ClearState;

            Logger.LoggerProvider = new LoggerProvider(_config, _powertoolsConfigurations, _systemWrapper);

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
            CaptureExtractedKeyPaths(eventObject);
            if (logEvent || _powertoolsConfigurations.LoggerLogEvent)
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
        return LogLevel.Debug >= _powertoolsConfigurations.GetLogLevel(_config.MinimumLevel);
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
            .Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Replace("Root=", "");

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
        CaptureKeyFromPath(eventArg, _correlationIdPath, LoggingConstants.KeyCorrelationId);
    }

    /// <summary>
    ///     Captures the extracted key paths.
    /// </summary>
    /// <param name="eventArg">The event argument.</param>
    private void CaptureExtractedKeyPaths(object eventArg)
    {
        if (!_extractedKeyPaths?.Any() ?? true)
            return;
        foreach (var (path, keyName) in _extractedKeyPaths)
        {
            CaptureKeyFromPath(eventArg, path, keyName);
        }
    }

    /// <summary>
    /// Captures an arbitrary value from the path and stores it in a key
    /// </summary>
    /// <param name="eventArg">The event argument.</param>
    /// <param name="path">The path to the input data.</param>
    /// <param name="keyName">The name of the key to store the data in.</param>
    private void CaptureKeyFromPath(object eventArg, string path, string keyName)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(keyName))
            return;

        var paths = path
            .Split(CorrelationIdPaths.Separator, StringSplitOptions.RemoveEmptyEntries);

        if (!paths.Any())
            return;

        if (eventArg is null)
        {
            if (IsDebug())
                _systemWrapper.LogLine(
                    $"Skipping {keyName} capture because event parameter not found.");
            return;
        }

        try
        {
            var keyValue = string.Empty;

            var jsonDoc =
                JsonDocument.Parse(PowertoolsLoggingSerializer.Serialize(eventArg, eventArg.GetType()));

            var element = jsonDoc.RootElement;

            for (var i = 0; i < paths.Length; i++)
            {
                // For casing parsing to be removed from Logging v2 when we get rid of outputcase
                // without this CorrelationIdPaths.ApiGatewayRest would not work
                var pathWithOutputCase =
                    _powertoolsConfigurations.ConvertToOutputCase(paths[i], _config.LoggerOutputCase);
                if (!element.TryGetProperty(pathWithOutputCase, out var childElement))
                    break;

                element = childElement;
                if (i == paths.Length - 1)
                    keyValue = element.ToString();
            }

            if (!string.IsNullOrWhiteSpace(keyValue))
                Logger.AppendKey(keyName, keyValue);
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
        LoggingLambdaContext.Clear();
        Logger.LoggerProvider = null;
        Logger.RemoveAllKeys();
        Logger.ClearLoggerInstance();
    }
}