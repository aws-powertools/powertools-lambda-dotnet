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
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Tracing.Internal;

/// <summary>
///     This aspect will automatically trace all function handlers.
///     Scope.Global is singleton
/// </summary>
[Aspect(Scope.Global)]
public class TracingAspect
{
    /// <summary>
    ///     The Powertools for AWS Lambda (.NET) configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     X-Ray Recorder
    /// </summary>
    private readonly IXRayRecorder _xRayRecorder;

    /// <summary>
    ///     If true, then is cold start
    /// </summary>
    private static bool _isColdStart = true;

    /// <summary>
    ///     If true, capture annotations
    /// </summary>
    private static bool _captureAnnotations = true;

    /// <summary>
    ///     If true, annotations have been captured
    /// </summary>
    private bool _isAnnotationsCaptured;

    /// <summary>
    ///     Tracing namespace
    /// </summary>
    private string _namespace;

    /// <summary>
    ///     The capture mode
    /// </summary>
    private TracingCaptureMode _captureMode;

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public TracingAspect()
    {
        _xRayRecorder = XRayRecorder.Instance;
        _powertoolsConfigurations = PowertoolsConfigurations.Instance;
    }

    /// <summary>
    /// the code is executed instead of the target method.
    /// The call to original method is wrapped around the following code
    /// the original code is called with var result = target(args);
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <param name="target"></param>
    /// <param name="triggers"></param>
    /// <returns></returns>
    [Advice(Kind.Around)]
    public object Around(
        [Argument(Source.Name)] string name,
        [Argument(Source.Arguments)] object[] args,
        [Argument(Source.Target)] Func<object[], object> target,
        [Argument(Source.Triggers)] Attribute[] triggers)
    {
        // Before running Function

        var trigger = triggers.OfType<TracingAttribute>().First();
        try
        {
            if (TracingDisabled())
                return target(args);

            _namespace = trigger.Namespace;

            var segmentName = !string.IsNullOrWhiteSpace(trigger.SegmentName) ? trigger.SegmentName : $"## {name}";
            var nameSpace = GetNamespace();

            _xRayRecorder.BeginSubsegment(segmentName);
            _xRayRecorder.SetNamespace(nameSpace);

            if (_captureAnnotations)
            {
                _xRayRecorder.AddAnnotation("ColdStart", _isColdStart);

                _captureAnnotations = false;
                _isAnnotationsCaptured = true;

                if (_powertoolsConfigurations.IsServiceDefined)
                    _xRayRecorder.AddAnnotation("Service", _powertoolsConfigurations.Service);
            }

            _isColdStart = false;

            // return of the handler
            var result = target(args);

            // must get capture after all subsegments run
            _captureMode = trigger.CaptureMode;

            if (CaptureResponse())
            {
                _xRayRecorder.AddMetadata
                (
                    nameSpace,
                    $"{name} response",
                    result
                );
            }

            // after 
            return result;
        }
        catch (Exception e)
        {
            _captureMode = trigger.CaptureMode;
            HandleException(e, name);
            throw;
        }
    }

    /// <summary>
    /// the code is injected after the method ends.
    /// </summary>
    [Advice(Kind.After)]
    public void OnExit()
    {
        if (TracingDisabled())
            return;

        if (_isAnnotationsCaptured)
            _captureAnnotations = true;

        _xRayRecorder.EndSubsegment();
    }

    /// <summary>
    /// Code that handles when exceptions occur in the client method
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="name"></param>
    private void HandleException(Exception exception, string name)
    {
        if (CaptureError())
        {
            var nameSpace = GetNamespace();

            var sb = new StringBuilder();
            sb.AppendLine($"Exception type: {exception.GetType()}");
            sb.AppendLine($"Exception message: {exception.Message}");
            sb.AppendLine($"Stack trace: {exception.StackTrace}");

            if (exception.InnerException != null)
            {
                sb.AppendLine("---BEGIN InnerException--- ");
                sb.AppendLine($"Exception type {exception.InnerException.GetType()}");
                sb.AppendLine($"Exception message: {exception.InnerException.Message}");
                sb.AppendLine($"Stack trace: {exception.InnerException.StackTrace}");
                sb.AppendLine("---END Inner Exception");
            }

            _xRayRecorder.AddMetadata
            (
                nameSpace,
                $"{name} error",
                sb.ToString()
            );
        }

        // // The purpose of ExceptionDispatchInfo.Capture is to capture a potentially mutating exception's StackTrace at a point in time:
        // // https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions#capture-exceptions-to-rethrow-later
        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    /// <summary>
    ///     Gets the namespace.
    /// </summary>
    /// <returns>System.String.</returns>
    private string GetNamespace()
    {
        return !string.IsNullOrWhiteSpace(_namespace) ? _namespace : _powertoolsConfigurations.Service;
    }

    /// <summary>
    /// Method that checks if tracing is disabled
    /// </summary>
    /// <returns></returns>
    private bool TracingDisabled()
    {
        if (_powertoolsConfigurations.TracingDisabled)
        {
            Console.WriteLine("Tracing has been disabled via env var POWERTOOLS_TRACE_DISABLED");
            return true;
        }

        if (!_powertoolsConfigurations.IsLambdaEnvironment)
        {
            Console.WriteLine("Running outside Lambda environment; disabling Tracing");
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Captures the response.
    /// </summary>
    /// <returns><c>true</c> if tracing should capture responses, <c>false</c> otherwise.</returns>
    private bool CaptureResponse()
    {
        switch (_captureMode)
        {
            case TracingCaptureMode.EnvironmentVariable:
                return _powertoolsConfigurations.TracerCaptureResponse;
            case TracingCaptureMode.Response:
            case TracingCaptureMode.ResponseAndError:
                return true;
            case TracingCaptureMode.Error:
            case TracingCaptureMode.Disabled:
            default:
                return false;
        }
    }

    /// <summary>
    ///     Captures the error.
    /// </summary>
    /// <returns><c>true</c> if tracing should capture errors, <c>false</c> otherwise.</returns>
    private bool CaptureError()
    {
        if (TracingDisabled())
            return false;

        switch (_captureMode)
        {
            case TracingCaptureMode.EnvironmentVariable:
                return _powertoolsConfigurations.TracerCaptureError;
            case TracingCaptureMode.Error:
            case TracingCaptureMode.ResponseAndError:
                return true;
            case TracingCaptureMode.Response:
            case TracingCaptureMode.Disabled:
            default:
                return false;
        }
    }

    /// <summary>
    ///     Resets static variables for test.
    /// </summary>
    internal static void ResetForTest()
    {
        _isColdStart = true;
        _captureAnnotations = true;
    }
}