using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AWS.Lambda.PowerTools.Metrics.Model
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
        public MetricUnit Unit {
            get;
            set;
        }

        public MetricDefinition(string name, double value) : this(name, MetricUnit.NONE, new List<double> { value })
        {
        }

        public MetricDefinition(string name, MetricUnit unit, double value) : this(name, unit, new List<double> { value }) { }

        public MetricDefinition(string name, MetricUnit unit, List<double> values)
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