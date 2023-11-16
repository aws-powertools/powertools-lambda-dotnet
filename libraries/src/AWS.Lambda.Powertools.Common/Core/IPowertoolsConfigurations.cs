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

namespace AWS.Lambda.Powertools.Common;

/// <summary>
///     Interface IPowertoolsConfigurations
/// </summary>
public interface IPowertoolsConfigurations
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
    ///     Gets the Powertools log level.
    /// </summary>
    /// <value>The log level.</value>
    string LogLevel { get; }
    
    /// <summary>
    ///     Gets the AWS Lambda log level.
    /// </summary>
    /// <value>The log level.</value>
    string AWSLambdaLogLevel { get; }

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
    ///     Gets the logger output casing.
    /// </summary>
    /// <value>The logger output casing. Defaults to snake case.</value>
    string LoggerOutputCase { get; }

    /// <summary>
    ///     Gets the X-Ray trace identifier.
    /// </summary>
    /// <value>The X-Ray trace identifier.</value>
    string XRayTraceId { get; }
    
    /// <summary>
    ///     Gets a value indicating whether this instance is Lambda.
    /// </summary>
    /// <value><c>true</c> if this instance is Lambda; otherwise, <c>false</c>.</value>
    bool IsLambdaEnvironment { get; }
    
    /// <summary>
    ///     Gets a value indicating whether [tracing is disabled].
    /// </summary>
    /// <value><c>true</c> if [tracing is disabled]; otherwise, <c>false</c>.</value>
    bool TracingDisabled { get; }

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
    
    /// <summary>
    /// Sets the execution Environment Variable (AWS_EXECUTION_ENV)
    /// </summary>
    /// <param name="type"></param>
    void SetExecutionEnvironment<T>(T type);
    
    /// <summary>
    ///     Gets a value indicating whether [Idempotency is disabled].
    /// </summary>
    /// <value><c>true</c> if [Idempotency is disabled]; otherwise, <c>false</c>.</value>
    bool IdempotencyDisabled { get; }

    /// <summary>
    /// Gets the error handling policy to apply during batch processing.
    /// </summary>
    /// <value>Defaults to 'DeriveFromEvent'.</value>
    string BatchProcessingErrorHandlingPolicy { get; }

    /// <summary>
    /// Gets a value indicating whether Batch processing in parallel is enabled.
    /// </summary>
    /// <value>Defaults to false</value>
    bool BatchParallelProcessingEnabled { get; }
    
    /// <summary>
    /// Gets the maximum degree of parallelism to apply during batch processing.
    /// </summary>
    /// <value>Defaults to 1 (no parallelism). Specify -1 to automatically use the value of <see cref="System.Environment.ProcessorCount">ProcessorCount</see>.</value>
    int BatchProcessingMaxDegreeOfParallelism { get; }
    
    
}