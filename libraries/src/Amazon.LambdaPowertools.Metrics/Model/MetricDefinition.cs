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
        public Unit Unit {
            get;
            set;
        }

        public MetricDefinition(string name, double value) : this(name, Unit.NONE, new List<double> { value })
        {
        }

        public MetricDefinition(string name, Unit unit, double value) : this(name, unit, new List<double> { value }) { }

        public MetricDefinition(string name, Unit unit, List<double> values)
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