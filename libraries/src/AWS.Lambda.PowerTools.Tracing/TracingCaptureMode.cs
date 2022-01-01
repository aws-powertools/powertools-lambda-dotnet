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

namespace Amazon.Lambda.PowerTools.Tracing;

/// <summary>
///     Enum TracingCaptureMode
/// </summary>
public enum TracingCaptureMode
{
    /// <summary>
    ///     Enables attribute to capture only response. If this mode is explicitly overridden
    ///     on {<see cref="T:Amazon.Lambda.PowerTools.Tracing.TracingAttribute" /> attribute, it will override value of
    ///     environment variable POWERTOOLS_TRACER_CAPTURE_RESPONSE
    /// </summary>
    RESPONSE,

    /// <summary>
    ///     Enabled attribute to capture only error from the method. If this mode is explicitly overridden
    ///     on <see cref="T:Amazon.Lambda.PowerTools.Tracing.TracingAttribute" /> attribute, it will override value of
    ///     environment variable POWERTOOLS_TRACER_CAPTURE_ERROR
    /// </summary>
    ERROR,

    /// <summary>
    ///     Enabled attribute to capture both response error from the method. If this mode is explicitly overridden
    ///     on <see cref="T:Amazon.Lambda.PowerTools.Tracing.TracingAttribute" /> attribute, it will override value of
    ///     environment variables POWERTOOLS_TRACER_CAPTURE_RESPONSE
    ///     and POWERTOOLS_TRACER_CAPTURE_ERROR
    /// </summary>
    RESPONSE_AND_ERROR,

    /// <summary>
    ///     Disables attribute to capture both response and error from the method. If this mode is explicitly overridden
    ///     on <see cref="T:Amazon.Lambda.PowerTools.Tracing.TracingAttribute" /> attribute, it will override values of
    ///     environment variable POWERTOOLS_TRACER_CAPTURE_RESPONSE
    ///     and POWERTOOLS_TRACER_CAPTURE_ERROR
    /// </summary>
    DISABLED,

    /// <summary>
    ///     Enables/Disables attribute to capture response and error from the method based on the value of
    ///     environment variable POWERTOOLS_TRACER_CAPTURE_RESPONSE and POWERTOOLS_TRACER_CAPTURE_ERROR
    /// </summary>
    ENVIRONMENT_VARIABLE
}