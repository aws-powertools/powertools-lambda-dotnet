using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{

    public class MetricsManager : IDisposable
    {
        [MaxLength(9)]
        private Dictionary<string, string> _dimensions;
        public Dictionary<string, string> Dimensions
        {
            get { return _dimensions; }
        }

        [MaxLength(100)]
        private Dictionary<string, List<Metric>> _metrics;
        public Dictionary<string, List<Metric>> Metrics
        {
            get { return _metrics; }
        }

        private Dictionary<string, dynamic> _metadata;
        
        public Dictionary<string, dynamic> Metadata
        {
            get { return _metadata; }
        }

        private bool disposedValue;
        private static int _metricCount = 0;

        public MetricsManager(
                        string metricsNamespace = null,
                        string serviceName = null,
                        Dictionary<string, string> dimensions = null,
                        Dictionary<string, List<Metric>> metrics = null,
                        Dictionary<string, dynamic> metadata = null)
        {
            PowertoolsSettings.SetNamespace(metricsNamespace);
            PowertoolsSettings.SetService(serviceName);

            _dimensions = dimensions != null ? dimensions : new Dictionary<string, string>();
            _metrics = metrics != null ? metrics : new Dictionary<string, List<Metric>>();
            _metadata = metadata != null ? metadata : new Dictionary<string, dynamic>();

            if(!string.IsNullOrEmpty(PowertoolsSettings.Service)){
                AddDimension("service", PowertoolsSettings.Service);
            }
        }

        public void AddDimension(string key, string value)
        {
            if (_dimensions.ContainsKey(key))
            {
                Console.WriteLine($"Dimension '{key}' already exists. Skipping...");
                return;
            }

            if(_dimensions.Count == 9)
            {
                Console.WriteLine($"Maximum number of dimensions (9) reached. Cannot add '{key}' to dimensions.");
                return;
            }

            _dimensions.Add(key, value);
        }

        public virtual void AddMetric(Metric metric)
        {
            if (_metrics.ContainsKey(metric.Name))
            {
                _metrics[metric.Name].Add(metric);
                _metricCount++;
            }
            else {
                _metrics.Add(metric.Name, new List<Metric>{metric});
                _metricCount++;
            }

            if (_metricCount == 100)
            {
                Flush();
            }
        }

        public void AddMetadata(string key, dynamic value)
        {

            if (_metadata.ContainsKey(key))
            {
                Console.WriteLine($"Metadata '{key}' already exists. Skipping...");
                return;
            }

            _metadata.Add(key, value);
        }

        //TODO: Turn this into a private method once decision has been made on the implementation
        public void Flush()
        {
            var metricObj = new MetricsWrapper
            {
                Root = new MetricsDefinition
                {
                    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    CloudWatchMetrics = new List<CloudWatchMetrics>{
                        new CloudWatchMetrics{
                            Namespace = PowertoolsSettings.Namespace,
                            Dimensions = new List<List<string>>{
                                ExtractDimensions(Dimensions)
                            },
                            Metrics = ExtractMetricDefinitionSet(Metrics)
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(metricObj, typeof(MetricsWrapper));

            string result = AddToJson(json, _dimensions, _metadata, _metrics);

            Console.WriteLine(result);

            _metrics.Clear();
        }

        private List<string> ExtractDimensions(Dictionary<string, string> dimensions)
        {
            List<string> result = new List<string>();

            result.AddRange(dimensions.Keys);

            return result;
        }

        private List<MetricsDefinitionSet> ExtractMetricDefinitionSet(Dictionary<string, List<Metric>> metrics)
        {
            List<MetricsDefinitionSet> metricDefinitionSet = new List<MetricsDefinitionSet>();

            foreach (var item in metrics)
            {
                metricDefinitionSet.Add(new MetricsDefinitionSet
                {
                    Name = item.Value[0].Name,
                    Unit = item.Value[0].Unit
                });
            }

            return metricDefinitionSet;
        }

        private string AddToJson(string json, Dictionary<string, string> dimensions, Dictionary<string, dynamic> metadata, Dictionary<string, List<Metric>> metrics)
        {
            string result = "";
            foreach (var item in dimensions)
            {
                result += $",\"{item.Key}\":\"{item.Value}\"";
            }

            foreach (var item in metadata)
            {
                result += $",\"{item.Key}\":\"{item.Value}\"";
            }

            foreach (var item in metrics.Values)
            {
                if(item.Count > 1){
                    string resultArray = $",\"{item[0].Name}\": [";

                    for (int i = 0; i < item.Count; i++)
                    {
                        if(i == item.Count - 1){
                            resultArray += $"{item[i].Value}]";
                        }
                        else {
                            resultArray += $"{item[i].Value},";
                        }
                    }

                    result += resultArray;
                }
                else {
                    result += $",\"{item[0].Name}\": {item[0].Value}";
                }
                
            }

            return $"{json.Remove(json.Length - 1)}{result}}}";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Flush();
                }

                _dimensions = null;
                _metrics = null;
                _metadata = null;
                disposedValue = true;
            }
        }

        ~MetricsManager()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
