using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Amazon.LambdaPowertools.Metrics.Model
{
    public class MetricDefinition
    {
        [JsonPropertyName("Name")]
        public string Name
        {
            get;
            set;
        }

        [JsonIgnore]
        public List<double> Values
        {
            get;
        }

        [JsonPropertyName("Unit")]
        public MetricsUnit Unit {
            get;
            set;
        }

        public MetricDefinition(string name, double value) : this(name, MetricsUnit.NONE, new List<double> { value })
        {
        }

        public MetricDefinition(string name, MetricsUnit unit, double value) : this(name, unit, new List<double> { value }) { }

        public MetricDefinition(string name, MetricsUnit unit, List<double> values)
        {
            Name = name;
            Unit = unit;
            Values = values;
        }

        public void AddValue(double value)
        {
            Values.Add(value);
        }        
    }
}