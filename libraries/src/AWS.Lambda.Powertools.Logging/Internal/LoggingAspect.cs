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
using System.Runtime.ExceptionServices;
using System.Text.Json;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Logging Aspect
///     Scope.Global is singleton
/// </summary>
/// <seealso cref="IMethodAspectHandler" />
public class LoggingAspect : IMethodAspectHandler
{
    /// <summary>
    ///     The initialize context
    /// </summary>
    private bool _initializeContext = true;

    /// <summary>
    ///     Clear state?
    /// </summary>
    private bool _clearState;

    /// <summary>
    ///     The is context initialized
    /// </summary>
    private bool _isContextInitialized;

    /// <summary>
    ///     Specify to clear Lambda Context on exit
    /// </summary>
    private bool _clearLambdaContext;

    private ILogger _logger;
    private bool _isDebug;
    private bool _bufferingEnabled;
    private PowertoolsLoggerConfiguration _currentConfig;
    private bool _flushBufferOnUncaughtError;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggingAspect" /> class.
    /// </summary>
    public LoggingAspect(ILogger logger)
    {
        _logger = logger ?? LoggerFactoryHolder.GetOrCreateFactory().CreatePowertoolsLogger();
    }

    private void InitializeLogger(LoggingAttribute trigger)
    {
        // Check which settings are explicitly provided in the attribute
        var hasLogLevel = trigger.LogLevel != LogLevel.None;
        var hasService = !string.IsNullOrEmpty(trigger.Service);
        var hasOutputCase = trigger.LoggerOutputCase != LoggerOutputCase.Default;
        var hasSamplingRate = trigger.SamplingRate > 0;

        // Only update configuration if any settings were provided
        var needsReconfiguration = hasLogLevel || hasService || hasOutputCase || hasSamplingRate;
        _currentConfig = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();

        if (needsReconfiguration)
        {
            // Apply each setting directly using the existing Logger static methods
            if (hasLogLevel) _currentConfig.MinimumLogLevel = trigger.LogLevel;
            if (hasService) _currentConfig.Service = trigger.Service;
            if (hasOutputCase) _currentConfig.LoggerOutputCase = trigger.LoggerOutputCase;
            if (hasSamplingRate) _currentConfig.SamplingRate = trigger.SamplingRate;

            // Need to refresh the logger after configuration changes
            _logger = LoggerFactoryHelper.CreateAndConfigureFactory(_currentConfig).CreatePowertoolsLogger();
            Logger.ClearInstance();
        }

        // Set operational flags based on current configuration
        _isDebug = _currentConfig.MinimumLogLevel <= LogLevel.Debug;
        _bufferingEnabled = _currentConfig.LogBuffering != null;
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
        if (LoggingLambdaContext.Instance is null && _isDebug)
            ConsoleWrapper.WriteLine(LogLevel.Warning.ToLambdaLogLevel(),
                "Skipping Lambda Context injection because ILambdaContext context parameter not found.");
    }

    /// <summary>
    ///     Captures the correlation identifier.
    /// </summary>
    /// <param name="eventArg">The event argument.</param>
    /// <param name="correlationIdPath"></param>
    private void CaptureCorrelationId(object eventArg, string correlationIdPath)
    {
        if (string.IsNullOrWhiteSpace(correlationIdPath))
            return;

        var correlationIdPaths = correlationIdPath
            .Split(CorrelationIdPaths.Separator, StringSplitOptions.RemoveEmptyEntries);

        if (!correlationIdPaths.Any())
            return;

        if (eventArg is null)
        {
            if (_isDebug)
                ConsoleWrapper.WriteLine(LogLevel.Warning.ToLambdaLogLevel(),
                    "Skipping CorrelationId capture because event parameter not found.");
            return;
        }

        try
        {
            var correlationId = string.Empty;

            var jsonDoc =
                JsonDocument.Parse(_currentConfig.Serializer.Serialize(eventArg, eventArg.GetType()));

            var element = jsonDoc.RootElement;

            for (var i = 0; i < correlationIdPaths.Length; i++)
            {
                // TODO: For casing parsing to be removed from Logging v2 when we get rid of outputcase without this CorrelationIdPaths.ApiGatewayRest would not work
                // TODO: This will be removed and replaced by JMesPath

                var pathWithOutputCase = correlationIdPaths[i].ToCase(_currentConfig.LoggerOutputCase);
                if (!element.TryGetProperty(pathWithOutputCase, out var childElement))
                    break;

                element = childElement;
                if (i == correlationIdPaths.Length - 1)
                    correlationId = element.ToString();
            }

            if (!string.IsNullOrWhiteSpace(correlationId))
                _logger.AppendKey(LoggingConstants.KeyCorrelationId, correlationId);
        }
        catch (Exception e)
        {
            if (_isDebug)
                ConsoleWrapper.WriteLine(LogLevel.Warning.ToLambdaLogLevel(),
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
                if (_isDebug)
                    ConsoleWrapper.WriteLine(LogLevel.Warning.ToLambdaLogLevel(),
                        "Skipping Event Log because event parameter not found.");
                break;
            }
            case Stream:
                try
                {
                    _logger.LogInformation(eventArg);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to log event from supplied input stream.");
                }

                break;
            default:
                try
                {
                    _logger.LogInformation(eventArg);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to log event from supplied input object.");
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
    }

    /// <summary>
    /// Entry point for the aspect.
    /// </summary>
    /// <param name="eventArgs"></param>
    public void OnEntry(AspectEventArgs eventArgs)
    {
        var trigger = eventArgs.Triggers.OfType<LoggingAttribute>().First();
        try
        {
            _clearState = trigger.ClearState;

            InitializeLogger(trigger);

            if (!_initializeContext)
                return;

            _initializeContext = false;
            _isContextInitialized = true;
            _flushBufferOnUncaughtError = trigger.FlushBufferOnUncaughtError;

            var eventObject = eventArgs.Args.FirstOrDefault();
            CaptureLambdaContext(eventArgs);
            CaptureCorrelationId(eventObject, trigger.CorrelationIdPath);

            switch (trigger.IsLogEventSet)
            {
                case true when trigger.LogEvent:
                case false when _currentConfig.LogEvent:
                    LogEvent(eventObject);
                    break;
            }
        }
        catch (Exception exception)
        {
            if (_bufferingEnabled && _flushBufferOnUncaughtError)
            {
                _logger.FlushBuffer();
            }

            // The purpose of ExceptionDispatchInfo.Capture is to capture a potentially mutating exception's StackTrace at a point in time:
            // https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions#capture-exceptions-to-rethrow-later
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    /// <summary>
    /// When the method returns successfully, this method is called.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <param name="result"></param>
    public void OnSuccess(AspectEventArgs eventArgs, object result)
    {
        
    }

    /// <summary>
    /// When the method throws an exception, this method is called.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <param name="exception"></param>
    public void OnException(AspectEventArgs eventArgs, Exception exception)
    {
        if (_bufferingEnabled && _flushBufferOnUncaughtError)
        {
            _logger.FlushBuffer();
        }
        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    /// <summary>
    /// WHen the method exits, this method is called even if it throws an exception.
    /// </summary>
    /// <param name="eventArgs"></param>
    public void OnExit(AspectEventArgs eventArgs)
    {
        if (!_isContextInitialized)
            return;
        if (_clearLambdaContext)
            LoggingLambdaContext.Clear();
        if (_clearState)
            _logger.RemoveAllKeys();
        _initializeContext = true;
        
        if (_bufferingEnabled)
        {
            // clear the buffer after the handler has finished
            _logger.ClearBuffer();
        }
    }
}