using System.Collections.Generic;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
/// Configuration options for AWS Lambda Powertools Metrics.
/// </summary>
public class MetricsOptions
{
    /// <summary>
    /// Gets or sets the CloudWatch metrics namespace.
    /// </summary>
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the service name to be used as a metric dimension.
    /// </summary>
    public string Service { get; set; }

    /// <summary>
    /// Gets or sets whether to throw an exception when no metrics are emitted.
    /// </summary>
    public bool? RaiseOnEmptyMetrics { get; set; }

    /// <summary>
    /// Gets or sets whether to capture cold start metrics.
    /// </summary>
    public bool? CaptureColdStart { get; set; }

    /// <summary>
    /// Gets or sets the default dimensions to be added to all metrics.
    /// </summary>
    public Dictionary<string, string> DefaultDimensions { get; set; }
}