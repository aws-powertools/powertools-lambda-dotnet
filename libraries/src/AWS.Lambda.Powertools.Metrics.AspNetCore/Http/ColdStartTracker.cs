using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Common;
using Microsoft.AspNetCore.Http;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Http;


/// <summary>
/// Tracks and manages cold start metrics for Lambda functions in ASP.NET Core applications.
/// </summary>
/// <remarks>
/// This class is responsible for detecting and recording the first invocation (cold start) of a Lambda function.
/// It ensures thread-safe tracking of cold starts and proper metric capture using the provided IMetrics implementation.
/// The cold start state is shared by all instances and is recorded once per execution environment.
/// Execution environments initialized for provisioned concurrency are not cold starts, matching the
/// <c>[Metrics]</c> attribute.
/// </remarks>
internal class ColdStartTracker
{
    private readonly IMetrics _metrics;
    private static bool _coldStart = true;
    private static readonly object _lock = new();
    private const string ProvisionedConcurrency = "provisioned-concurrency";

    /// <summary>
    /// Initializes a new instance of the <see cref="ColdStartTracker"/> class.
    /// </summary>
    /// <param name="metrics">The metrics implementation to use for capturing cold start metrics.</param>
    public ColdStartTracker(IMetrics metrics)
    {
        _metrics = metrics;
    }

    /// <summary>
    /// Tracks the cold start of the Lambda function.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    internal void TrackColdStart(HttpContext context)
    {
        if (!_coldStart) return;

        lock (_lock)
        {
            if (!_coldStart) return;
            // Mark as consumed first so a failing capture is not retried on every request
            _coldStart = false;

            if (PowertoolsConfigurations.Instance.AwsInitializationType == ProvisionedConcurrency) return;

            _metrics.CaptureColdStartMetric(context.Items["LambdaContext"] as ILambdaContext);
        }
    }

    /// <summary>
    /// Resets the cold start tracking state. Intended for tests only.
    /// </summary>
    internal static void ResetColdStart()
    {
        lock (_lock)
        {
            _coldStart = true;
        }
    }
}