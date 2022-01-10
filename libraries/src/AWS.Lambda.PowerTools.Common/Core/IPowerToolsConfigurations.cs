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

namespace AWS.Lambda.PowerTools.Common;

/// <summary>
///     Interface IPowerToolsConfigurations
/// </summary>
public interface IPowerToolsConfigurations
{
    /// <summary>
    ///     Gets the service.
    /// </summary>
    /// <value>The service.</value>
    string Service { get; }

    /// <summary>
    ///     Gets a value indicating whether this instance is service defined.
    /// </summary>
    /// <value><c>true</c> if this instance is service defined; otherwise, <c>false</c>.</value>
    bool IsServiceDefined { get; }

    /// <summary>
    ///     Gets a value indicating whether [tracer capture response].
    /// </summary>
    /// <value><c>true</c> if [tracer capture response]; otherwise, <c>false</c>.</value>
    bool TracerCaptureResponse { get; }

    /// <summary>
    ///     Gets a value indicating whether [tracer capture error].
    /// </summary>
    /// <value><c>true</c> if [tracer capture error]; otherwise, <c>false</c>.</value>
    bool TracerCaptureError { get; }

    /// <summary>
    ///     Gets a value indicating whether this instance is sam local.
    /// </summary>
    /// <value><c>true</c> if this instance is sam local; otherwise, <c>false</c>.</value>
    bool IsSamLocal { get; }

    /// <summary>
    ///     Gets the metrics namespace.
    /// </summary>
    /// <value>The metrics namespace.</value>
    string MetricsNamespace { get; }

    /// <summary>
    ///     Gets the log level.
    /// </summary>
    /// <value>The log level.</value>
    string LogLevel { get; }

    /// <summary>
    ///     Gets the logger sample rate.
    /// </summary>
    /// <value>The logger sample rate.</value>
    double? LoggerSampleRate { get; }

    /// <summary>
    ///     Gets a value indicating whether [logger log event].
    /// </summary>
    /// <value><c>true</c> if [logger log event]; otherwise, <c>false</c>.</value>
    bool LoggerLogEvent { get; }

    /// <summary>
    ///     Gets the X-Ray trace identifier.
    /// </summary>
    /// <value>The X-Ray trace identifier.</value>
    string XRayTraceId { get; }

    /// <summary>
    ///     Gets the environment variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>System.String.</returns>
    string GetEnvironmentVariable(string variable);

    /// <summary>
    ///     Gets the environment variable or default.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>System.String.</returns>
    string GetEnvironmentVariableOrDefault(string variable, string defaultValue);

    /// <summary>
    ///     Gets the environment variable or default.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="defaultValue">if set to <c>true</c> [default value].</param>
    /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
    bool GetEnvironmentVariableOrDefault(string variable, bool defaultValue);
}