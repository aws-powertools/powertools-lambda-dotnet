using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Common.Core;

namespace AWS.Lambda.Powertools.Common;

/// <summary>
///     Class PowertoolsConfigurations.
///     Implements the <see cref="IPowertoolsConfigurations" />
/// </summary>
/// <seealso cref="IPowertoolsConfigurations" />
public class PowertoolsConfigurations : IPowertoolsConfigurations
{
    private readonly IPowertoolsEnvironment _powertoolsEnvironment;

    /// <summary>
    ///     The maximum dimensions
    /// </summary>
    public const int MaxDimensions = 29;

    /// <summary>
    ///     The maximum metrics
    /// </summary>
    public const int MaxMetrics = 100;

    /// <summary>
    ///     The instance
    /// </summary>
    private static IPowertoolsConfigurations _instance;

    /// <summary>
    ///     Whether LambdaTraceProvider is available in the loaded Amazon.Lambda.Core assembly.
    ///     0 = not yet checked, 1 = available, -1 = unavailable.
    ///     Stored as int for atomic reads/writes via Volatile.
    /// </summary>
    private static int _traceProviderState; // 0 = unknown, 1 = available, -1 = unavailable

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowertoolsConfigurations" /> class.
    /// </summary>
    /// <param name="powertoolsEnvironment"></param>
    internal PowertoolsConfigurations(IPowertoolsEnvironment powertoolsEnvironment)
    {
        _powertoolsEnvironment = powertoolsEnvironment;
    }

    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static IPowertoolsConfigurations Instance =>
        _instance ??= new PowertoolsConfigurations(PowertoolsEnvironment.Instance);

    /// <summary>
    ///     Gets the environment variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>System.String.</returns>
    public string GetEnvironmentVariable(string variable)
    {
        return _powertoolsEnvironment.GetEnvironmentVariable(variable);
    }

    /// <summary>
    ///     Gets the environment variable or default.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>System.String.</returns>
    public string GetEnvironmentVariableOrDefault(string variable, string defaultValue)
    {
        var result = _powertoolsEnvironment.GetEnvironmentVariable(variable);
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
        var result = _powertoolsEnvironment.GetEnvironmentVariable(variable);
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
        return bool.TryParse(_powertoolsEnvironment.GetEnvironmentVariable(variable), out var result)
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

    /// <inheritdoc />
    public string LogLevel => GetEnvironmentVariable(Constants.LogLevelNameEnv);

    /// <inheritdoc />
    public string AWSLambdaLogLevel => GetEnvironmentVariable(Constants.AWSLambdaLogLevelNameEnv);

    /// <summary>
    ///     Gets the logger sample rate.
    /// </summary>
    /// <value>The logger sample rate.</value>
    public double LoggerSampleRate =>
        double.TryParse(_powertoolsEnvironment.GetEnvironmentVariable(Constants.LoggerSampleRateNameEnv),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;

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
    ///     Uses LambdaTraceProvider.CurrentTraceId when available (Amazon.Lambda.Core >= 2.8.0)
    ///     for correct trace ID isolation in concurrent Lambda executions (LMI).
    ///     Falls back to the _X_AMZN_TRACE_ID environment variable for older runtimes.
    /// </summary>
    /// <value>The X-Ray trace identifier.</value>
    public string XRayTraceId => GetTraceId(() => GetEnvironmentVariable(Constants.XrayTraceIdEnv));

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
    public bool IdempotencyDisabled =>
        GetEnvironmentVariableOrDefault(Constants.IdempotencyDisabledEnv, false);

    /// <inheritdoc />
    public string BatchProcessingErrorHandlingPolicy =>
        GetEnvironmentVariableOrDefault(Constants.BatchErrorHandlingPolicyEnv, "DeriveFromEvent");

    /// <inheritdoc />
    public bool BatchParallelProcessingEnabled =>
        GetEnvironmentVariableOrDefault(Constants.BatchParallelProcessingEnabled, false);

    /// <inheritdoc />
    public int BatchProcessingMaxDegreeOfParallelism =>
        GetEnvironmentVariableOrDefault(Constants.BatchMaxDegreeOfParallelismEnv, 1);

    /// <inheritdoc />
    public bool BatchThrowOnFullBatchFailureEnabled =>
        GetEnvironmentVariableOrDefault(Constants.BatchThrowOnFullBatchFailureEnv, true);

    /// <inheritdoc />
    public bool MetricsDisabled => GetEnvironmentVariableOrDefault(Constants.PowertoolsMetricsDisabledEnv, false);

    /// <inheritdoc />
    public bool IsColdStart => LambdaLifecycleTracker.IsColdStart;
    
    /// <inheritdoc />
    public string AwsInitializationType =>
        GetEnvironmentVariable(Constants.AWSInitializationTypeEnv);

    private static string GetTraceId(Func<string> fallback)
    {
        var state = Volatile.Read(ref _traceProviderState);

        if (state == 1)
            return GetTraceIdFromProvider();

        if (state == -1)
            return fallback();

        // First call — probe whether LambdaTraceProvider exists in the loaded runtime
        try
        {
            var traceId = GetTraceIdFromProvider();
            Volatile.Write(ref _traceProviderState, 1);
            return traceId;
        }
        catch (TypeLoadException)
        {
            Volatile.Write(ref _traceProviderState, -1);
            return fallback();
        }
    }

    /// <summary>
    ///     Isolated call to LambdaTraceProvider.CurrentTraceId.
    ///     Must not be inlined so that the TypeLoadException is thrown only
    ///     when this method is invoked, not when the caller is compiled.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GetTraceIdFromProvider()
    {
        return LambdaTraceProvider.CurrentTraceId;
    }
}