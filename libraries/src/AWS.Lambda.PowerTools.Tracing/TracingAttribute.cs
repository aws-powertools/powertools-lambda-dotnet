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

using Amazon.Lambda.PowerTools.Tracing.Internal;
using AWS.Lambda.PowerTools.Aspects;
using AWS.Lambda.PowerTools.Core;

namespace Amazon.Lambda.PowerTools.Tracing
{
    public class TracingAttribute : MethodAspectAttribute
    {
        /// <summary>
        /// Set custom segment name for the operation.
        /// The default is '## {MethodName}'.
        /// </summary>
        public string SegmentName { get; set; } = "";
        
        /// <summary>
        /// Set namespace to current subsegment.
        /// The default is the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
        /// </summary>
        public string Namespace { get; set; } = "";
        
        /// <summary>
        /// Set capture mode to record method responses and exceptions.
        /// The defaults are the environment variables <c>POWERTOOLS_TRACER_CAPTURE_RESPONSE</c> and <c>POWERTOOLS_TRACER_CAPTURE_ERROR</c>.
        /// </summary>
        public TracingCaptureMode CaptureMode { get; set; } = TracingCaptureMode.EnvironmentVariable;

        protected override IMethodAspectHandler CreateHandler()
        {
            return new TracingAspectHandler
            (
                SegmentName,
                Namespace,
                CaptureMode,
                PowerToolsConfigurations.Instance,
                XRayRecorder.Instance
            );
        }
    }
}