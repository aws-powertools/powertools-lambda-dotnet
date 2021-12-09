using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AWS.Lambda.PowerTools.Metrics
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

        /// <summary>
        /// Creates a MetricDefinition object. MetricUnit is set to NONE since it is not provided.
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Metric value</param>
        public MetricDefinition(string name, double value) : this(name, MetricUnit.NONE, new List<double> { value })
        {
        }

        /// <summary>
        /// Creates a MetricDefinition object
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="unit">Metric unit</param>
        /// <param name="value">Metric value</param>
        public MetricDefinition(string name, MetricUnit unit, double value) : this(name, unit, new List<double> { value }) { }

        /// <summary>
        /// Creates a MetricDefinition object with multiple values
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="unit">Metric unit</param>
        /// <param name="values">List of metric values</param>
        public MetricDefinition(string name, MetricUnit unit, List<double> values)
        {
            Name = name;
            Unit = unit;
            Values = values;
        }

        /// <summary>
        /// Adds value to existing metric with same key
        /// </summary>
        /// <param name="value">Metric value to add to existing key</param>
        public void AddValue(double value)
        {
            Values.Add(value);
        }        
    }
}