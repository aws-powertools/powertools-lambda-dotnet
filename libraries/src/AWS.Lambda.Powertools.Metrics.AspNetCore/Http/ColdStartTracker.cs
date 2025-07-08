using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Http;

namespace AWS.Lambda.Powertools.Metrics.AspNetCore.Http;


/// <summary>
/// Tracks and manages cold start metrics for Lambda functions in ASP.NET Core applications.
/// </summary>
/// <remarks>
/// This class is responsible for detecting and recording the first invocation (cold start) of a Lambda function.
/// It ensures thread-safe tracking of cold starts and proper metric capture using the provided IMetrics implementation.
/// </remarks>
internal class ColdStartTracker : IDisposable
{
    private readonly IMetrics _metrics;
    private static bool _coldStart = true;
    private static readonly object _lock = new();

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
            _metrics.CaptureColdStartMetric(context.Items["LambdaContext"] as ILambdaContext);
            _coldStart = false;
        }
    }

    /// <summary>
    /// Resets the cold start tracking state.
    /// </summary>
    internal static void ResetColdStart()
    {
        lock (_lock)
        {
            _coldStart = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ResetColdStart();
    }
}