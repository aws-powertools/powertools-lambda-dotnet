using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
/// Enum MetricResolution
/// </summary>
#if NET8_0_OR_GREATER
[JsonConverter(typeof(JsonStringEnumConverter<MetricResolution>))]
#else
[JsonConverter(typeof(StringEnumConverter))]
#endif
public enum MetricResolution
{
    /// <summary>
    ///     Standard resolution, with data having a one-minute granularity
    /// </summary>
    Standard = 60,

    /// <summary>
    ///     High resolution, with data at a granularity of one second
    /// </summary>
    High = 1,
    
    /// <summary>
    /// When not set default to not sending metric resolution
    /// </summary>
    Default = 0
}