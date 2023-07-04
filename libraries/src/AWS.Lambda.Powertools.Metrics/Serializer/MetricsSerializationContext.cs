using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Metrics;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(List<double>))]
[JsonSerializable(typeof(MetricUnit))]
[JsonSerializable(typeof(MetricDefinition))]
[JsonSerializable(typeof(DimensionSet))]
[JsonSerializable(typeof(Metadata))]
[JsonSerializable(typeof(MetricDirective))]
[JsonSerializable(typeof(MetricResolution))]
[JsonSerializable(typeof(MetricsContext))]
[JsonSerializable(typeof(RootNode))]
public partial class MetricsSerializationContext : JsonSerializerContext
{
    
}