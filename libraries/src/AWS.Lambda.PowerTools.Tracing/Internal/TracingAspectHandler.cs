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

namespace Amazon.Lambda.PowerTools.Tracing.Internal;

/// <summary>
///     Class TracingAspectHandler.
///     Implements the <see cref="AWS.Lambda.PowerTools.Aspects.IMethodAspectHandler" />
/// </summary>
/// <seealso cref="AWS.Lambda.PowerTools.Aspects.IMethodAspectHandler" />
internal class TracingAspectHandler : IMethodAspectHandler
{
    /// <summary>
    ///     The is cold start
    /// </summary>
    private static bool _isColdStart = true;

    /// <summary>
    ///     The capture annotations
    /// </summary>
    private static bool _captureAnnotations = true;

    /// <summary>
    ///     The capture mode
    /// </summary>
    private readonly TracingCaptureMode _captureMode;

    /// <summary>
    ///     The namespace
    /// </summary>
    private readonly string _namespace;

    /// <summary>
    ///     The power tools configurations
    /// </summary>
    private readonly IPowerToolsConfigurations _powerToolsConfigurations;

    /// <summary>
    ///     The segment name
    /// </summary>
    private readonly string _segmentName;

    /// <summary>
    ///     The x ray recorder
    /// </summary>
    private readonly IXRayRecorder _xRayRecorder;

    /// <summary>
    ///     The is annotations captured
    /// </summary>
    private bool _isAnnotationsCaptured;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TracingAspectHandler" /> class.
    /// </summary>
    /// <param name="segmentName">Name of the segment.</param>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="captureMode">The capture mode.</param>
    /// <param name="powerToolsConfigurations">The power tools configurations.</param>
    /// <param name="xRayRecorder">The x ray recorder.</param>
    internal TracingAspectHandler
    (
        string segmentName,
        string nameSpace,
        TracingCaptureMode captureMode,
        IPowerToolsConfigurations powerToolsConfigurations,
        IXRayRecorder xRayRecorder
    )
    {
        _segmentName = segmentName;
        _namespace = nameSpace;
        _captureMode = captureMode;
        _powerToolsConfigurations = powerToolsConfigurations;
        _xRayRecorder = xRayRecorder;
    }

    /// <summary>
    ///     Handles the <see cref="E:Entry" /> event.
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.PowerTools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    public void OnEntry(AspectEventArgs eventArgs)
    {
        var segmentName = !string.IsNullOrWhiteSpace(_segmentName) ? _segmentName : $"## {eventArgs.Name}";
        var nameSpace = GetNamespace();

        _xRayRecorder.BeginSubsegment(segmentName);
        _xRayRecorder.SetNamespace(nameSpace);

        if (_captureAnnotations)
        {
            _xRayRecorder.AddAnnotation("ColdStart", _isColdStart);

            _isColdStart = false;
            _captureAnnotations = false;
            _isAnnotationsCaptured = true;

            if (_powerToolsConfigurations.IsServiceDefined)
                _xRayRecorder.AddAnnotation("Service", _powerToolsConfigurations.Service);
        }
    }

    /// <summary>
    ///     Called when [success].
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.PowerTools.Aspects.AspectEventArgs" /> instance containing the
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
    /// <typeparam name="T"></typeparam>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.PowerTools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    /// <param name="exception">The exception.</param>
    /// <returns>T.</returns>
    public T OnException<T>(AspectEventArgs eventArgs, Exception exception)
    {
        if (CaptureError())
        {
            var nameSpace = GetNamespace();

            _xRayRecorder.AddMetadata
            (
                nameSpace,
                $"{eventArgs.Name} error",
                exception
            );
        }

        throw exception;
    }

    /// <summary>
    ///     Handles the <see cref="E:Exit" /> event.
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.PowerTools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    public void OnExit(AspectEventArgs eventArgs)
    {
        if (_isAnnotationsCaptured)
            _captureAnnotations = true;

        if (!_powerToolsConfigurations.IsSamLocal)
            _xRayRecorder.EndSubsegment();
    }

    /// <summary>
    ///     Gets the namespace.
    /// </summary>
    /// <returns>System.String.</returns>
    private string GetNamespace()
    {
        return !string.IsNullOrWhiteSpace(_namespace) ? _namespace : _powerToolsConfigurations.Service;
    }

    /// <summary>
    ///     Captures the response.
    /// </summary>
    /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
    private bool CaptureResponse()
    {
        switch (_captureMode)
        {
            case TracingCaptureMode.ENVIRONMENT_VARIABLE:
                return _powerToolsConfigurations.TracerCaptureResponse;
            case TracingCaptureMode.RESPONSE:
            case TracingCaptureMode.RESPONSE_AND_ERROR:
                return true;
            case TracingCaptureMode.ERROR:
            case TracingCaptureMode.DISABLED:
            default:
                return false;
        }
    }

    /// <summary>
    ///     Captures the error.
    /// </summary>
    /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
    private bool CaptureError()
    {
        switch (_captureMode)
        {
            case TracingCaptureMode.ENVIRONMENT_VARIABLE:
                return _powerToolsConfigurations.TracerCaptureError;
            case TracingCaptureMode.ERROR:
            case TracingCaptureMode.RESPONSE_AND_ERROR:
                return true;
            case TracingCaptureMode.RESPONSE:
            case TracingCaptureMode.DISABLED:
            default:
                return false;
        }
    }
}