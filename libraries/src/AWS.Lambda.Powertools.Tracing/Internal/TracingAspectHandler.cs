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
using System.Runtime.ExceptionServices;
using System.Text;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Tracing.Internal;

/// <summary>
///     Class TracingAspectHandler.
///     Implements the <see cref="IMethodAspectHandler" />
/// </summary>
/// <seealso cref="IMethodAspectHandler" />
internal class TracingAspectHandler : IMethodAspectHandler
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
    private readonly string _namespace;
    
    /// <summary>
    ///     The capture mode
    /// </summary>
    private readonly TracingCaptureMode _captureMode;
    
    /// <summary>
    ///     The segment name
    /// </summary>
    private readonly string _segmentName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TracingAspectHandler" /> class.
    /// </summary>
    /// <param name="segmentName">Name of the segment.</param>
    /// <param name="nameSpace">The namespace.</param>
    /// <param name="captureMode">The capture mode.</param>
    /// <param name="powertoolsConfigurations">The Powertools for AWS Lambda (.NET) configurations.</param>
    /// <param name="xRayRecorder">The X-Ray recorder.</param>
    internal TracingAspectHandler
    (
        string segmentName,
        string nameSpace,
        TracingCaptureMode captureMode,
        IPowertoolsConfigurations powertoolsConfigurations,
        IXRayRecorder xRayRecorder
    )
    {
        _segmentName = segmentName;
        _namespace = nameSpace;
        _captureMode = captureMode;
        _powertoolsConfigurations = powertoolsConfigurations;
        _xRayRecorder = xRayRecorder;
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
        if(TracingDisabled())
            return;

        var segmentName = !string.IsNullOrWhiteSpace(_segmentName) ? _segmentName : $"## {eventArgs.Name}";
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
        if (CaptureResponse())
        {
            var nameSpace = GetNamespace();
            
            _xRayRecorder.AddMetadata
            (
                nameSpace,
                $"{eventArgs.Name} response",
                result
            );
        }
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
                $"{eventArgs.Name} error",
                sb.ToString()
            );
        }

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
        if(TracingDisabled())
            return;

        if (_isAnnotationsCaptured)
            _captureAnnotations = true;

        _xRayRecorder.EndSubsegment();
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
    ///     Captures the response.
    /// </summary>
    /// <returns><c>true</c> if tracing should capture responses, <c>false</c> otherwise.</returns>
    private bool CaptureResponse()
    {
        if(TracingDisabled())
            return false;
        
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
        if(TracingDisabled())
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
    ///     Tracing disabled.
    /// </summary>
    /// <returns><c>true</c> if tracing is disabled, <c>false</c> otherwise.</returns>
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
    ///     Resets static variables for test.
    /// </summary>
    internal static void ResetForTest()
    {
        _isColdStart = true;
        _captureAnnotations = true;
    }
}
