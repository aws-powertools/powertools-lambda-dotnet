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
///     Class PowertoolsConfigurations.
///     Implements the <see cref="IPowertoolsConfigurations" />
/// </summary>
/// <seealso cref="IPowertoolsConfigurations" />
public class PowertoolsConfigurations : IPowertoolsConfigurations
{
    /// <summary>
    ///     The maximum dimensions
    /// </summary>
    public const int MaxDimensions = 9;

    /// <summary>
    ///     The maximum metrics
    /// </summary>
    public const int MaxMetrics = 100;

    /// <summary>
    ///     The instance
    /// </summary>
    private static IPowertoolsConfigurations _instance;

    /// <summary>
    ///     The system wrapper
    /// </summary>
    private readonly ISystemWrapper _systemWrapper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowertoolsConfigurations" /> class.
    /// </summary>
    /// <param name="systemWrapper">The system wrapper.</param>
    internal PowertoolsConfigurations(ISystemWrapper systemWrapper)
    {
        _systemWrapper = systemWrapper;
    }

    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static IPowertoolsConfigurations Instance =>
        _instance ??= new PowertoolsConfigurations(SystemWrapper.Instance);

    /// <summary>
    ///     Gets the environment variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>System.String.</returns>
    public string GetEnvironmentVariable(string variable)
    {
        return _systemWrapper.GetEnvironmentVariable(variable);
    }

    /// <summary>
    ///     Gets the environment variable or default.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>System.String.</returns>
    public string GetEnvironmentVariableOrDefault(string variable, string defaultValue)
    {
        var result = _systemWrapper.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(result) ? defaultValue : result;
    }

    /// <summary>
    ///     Gets the environment variable or default.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>System.Int32.</returns>
    public int GetEnvironmentVariableOrDefault(string variable, int defaultValue)
    {
        var result = _systemWrapper.GetEnvironmentVariable(variable);
        return int.TryParse(result, out var parsedValue) ? parsedValue : defaultValue;
    }

    /// <summary>
    ///     Gets the environment variable or default.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="defaultValue">if set to <c>true</c> [default value].</param>
    /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
    public bool GetEnvironmentVariableOrDefault(string variable, bool defaultValue)
    {
        return bool.TryParse(_systemWrapper.GetEnvironmentVariable(variable), out var result)
            ? result
            : defaultValue;
    }

    /// <summary>
    ///     Gets the service.
    /// </summary>
    /// <value>The service.</value>
    public string Service =>
        GetEnvironmentVariableOrDefault(Constants.ServiceNameEnv, "service_undefined");

    /// <summary>
    ///     Gets a value indicating whether this instance is service defined.
    /// </summary>
    /// <value><c>true</c> if this instance is service defined; otherwise, <c>false</c>.</value>
    public bool IsServiceDefined =>
        !string.IsNullOrWhiteSpace(GetEnvironmentVariable(Constants.ServiceNameEnv));

    /// <summary>
    ///     Gets a value indicating whether [tracer capture response].
    /// </summary>
    /// <value><c>true</c> if [tracer capture response]; otherwise, <c>false</c>.</value>
    public bool TracerCaptureResponse =>
        GetEnvironmentVariableOrDefault(Constants.TracerCaptureResponseEnv, true);

    /// <summary>
    ///     Gets a value indicating whether [tracer capture error].
    /// </summary>
    /// <value><c>true</c> if [tracer capture error]; otherwise, <c>false</c>.</value>
    public bool TracerCaptureError =>
        GetEnvironmentVariableOrDefault(Constants.TracerCaptureErrorEnv, true);

    /// <summary>
    ///     Gets a value indicating whether this instance is sam local.
    /// </summary>
    /// <value><c>true</c> if this instance is sam local; otherwise, <c>false</c>.</value>
    public bool IsSamLocal =>
        GetEnvironmentVariableOrDefault(Constants.SamLocalEnv, false);

    /// <summary>
    ///     Gets the metrics namespace.
    /// </summary>
    /// <value>The metrics namespace.</value>
    public string MetricsNamespace =>
        GetEnvironmentVariable(Constants.MetricsNamespaceEnv);

    /// <summary>
    ///     Gets the log level.
    /// </summary>
    /// <value>The log level.</value>
    public string LogLevel =>
        GetEnvironmentVariableOrDefault(Constants.AwsLogLevelNameEnv, GetEnvironmentVariable(Constants.LogLevelNameEnv));

    /// <summary>
    ///     Gets the logger sample rate.
    /// </summary>
    /// <value>The logger sample rate.</value>
    public double? LoggerSampleRate =>
        double.TryParse(_systemWrapper.GetEnvironmentVariable(Constants.LoggerSampleRateNameEnv), out var result)
            ? result
            : null;

    /// <summary>
    ///     Gets a value indicating whether [logger log event].
    /// </summary>
    /// <value><c>true</c> if [logger log event]; otherwise, <c>false</c>.</value>
    public bool LoggerLogEvent =>
        GetEnvironmentVariableOrDefault(Constants.LoggerLogEventNameEnv, false);

    /// <summary>
    ///     Gets the logger output casing.
    /// </summary>
    /// <value>The logger output casing. Defaults to snake case.</value>
    public string LoggerOutputCase =>
        GetEnvironmentVariableOrDefault(Constants.LoggerOutputCaseEnv, "SnakeCase");

    /// <summary>
    ///     Gets the X-Ray trace identifier.
    /// </summary>
    /// <value>The X-Ray trace identifier.</value>
    public string XRayTraceId =>
        GetEnvironmentVariable(Constants.XrayTraceIdEnv);

    /// <summary>
    ///     Gets a value indicating whether this instance is Lambda.
    /// </summary>
    /// <value><c>true</c> if this instance is Lambda; otherwise, <c>false</c>.</value>
    public bool IsLambdaEnvironment => GetEnvironmentVariable(Constants.LambdaTaskRoot) is not null;
    
    /// <summary>
    ///     Gets a value indicating whether [tracing is disabled].
    /// </summary>
    /// <value><c>true</c> if [tracing is disabled]; otherwise, <c>false</c>.</value>
    public bool TracingDisabled =>
        GetEnvironmentVariableOrDefault(Constants.TracingDisabledEnv, false);

    /// <inheritdoc />
    public void SetExecutionEnvironment<T>(T type)
    {
        _systemWrapper.SetExecutionEnvironment(type);
    }

    /// <inheritdoc />
    public bool IdempotencyDisabled =>
        GetEnvironmentVariableOrDefault(Constants.IdempotencyDisabledEnv, false);

    /// <inheritdoc />
    public string BatchProcessingErrorHandlingPolicy => GetEnvironmentVariableOrDefault(Constants.BatchErrorHandlingPolicyEnv, "DeriveFromEvent");

    /// <inheritdoc />
    public bool BatchParallelProcessingEnabled => GetEnvironmentVariableOrDefault(Constants.BatchParallelProcessingEnabled, false);

    /// <inheritdoc />
    public int BatchProcessingMaxDegreeOfParallelism => GetEnvironmentVariableOrDefault(Constants.BatchMaxDegreeOfParallelismEnv, 1);
}