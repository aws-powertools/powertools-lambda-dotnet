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

namespace Amazon.Lambda.PowerTools.Tracing.Internal
{
    internal class TracingAspectHandler : IMethodAspectHandler
    {
        private readonly string _segmentName;
        private readonly string _namespace;
        private readonly TracingCaptureMode _captureMode;
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        private readonly IXRayRecorder _xRayRecorder;
        
        private static bool _isColdStart = true;
        private static bool _captureAnnotations = true;
        private bool _isAnnotationsCaptured;

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
        
        private string GetNamespace()
        {
            return !string.IsNullOrWhiteSpace(_namespace) ? _namespace : _powerToolsConfigurations.Service;
        }

        private bool CaptureResponse()
        {
            switch (_captureMode)
            {
                case TracingCaptureMode.EnvironmentVariable:
                    return _powerToolsConfigurations.TracerCaptureResponse;
                case TracingCaptureMode.Response:
                case TracingCaptureMode.ResponseAndError:
                    return true;
                case TracingCaptureMode.Error:
                case TracingCaptureMode.Disabled:
                default:
                    return false;
            }
        }

        private bool CaptureError()
        {
            switch (_captureMode)
            {
                case TracingCaptureMode.EnvironmentVariable:
                    return _powerToolsConfigurations.TracerCaptureError;
                case TracingCaptureMode.Error:
                case TracingCaptureMode.ResponseAndError:
                    return true;
                case TracingCaptureMode.Response:
                case TracingCaptureMode.Disabled:
                default:
                    return false;
            }
        }

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

        public void OnSuccess(AspectEventArgs eventArgs, object result)
        {
            if (CaptureResponse())
            {
                var nameSpace = GetNamespace();
    
                _xRayRecorder.AddMetadata
                (
                    nameSpace: nameSpace,
                    key: $"{eventArgs.Name} response",
                    value: result
                );
            }
        }

        public T OnException<T>(AspectEventArgs eventArgs, Exception exception)
        {
            if (CaptureError())
            {
                var nameSpace = GetNamespace();

                _xRayRecorder.AddMetadata
                (
                    nameSpace: nameSpace,
                    key: $"{eventArgs.Name} error",
                    value: exception
                );
            }

            throw exception;
        }

        public void OnExit(AspectEventArgs eventArgs)
        {
            if (_isAnnotationsCaptured)
                _captureAnnotations = true;

            if (!_powerToolsConfigurations.IsSamLocal)
                _xRayRecorder.EndSubsegment();
        }
    }
}