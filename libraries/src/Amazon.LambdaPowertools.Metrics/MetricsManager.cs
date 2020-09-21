using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{

    public class MetricsManager : IDisposable
    {
        private string _namespace;
        public string Namespace
        {
            get { return _namespace; }
            set { _namespace = value; }
        }

        private string _service;
        public string Service
        {
            get { return _service; }
            set { _service = value; }
        }

        [MaxLength(9)]
        private Dictionary<string, string> _dimensions;
        public Dictionary<string, string> Dimensions
        {
            get { return _dimensions; }
            set { _dimensions = value; }
        }

        [MaxLength(100)]
        private Dictionary<string, Metric> _metrics;
        public Dictionary<string, Metric> Metrics
        {
            get { return _metrics; }
            set { _metrics = value; }
        }

        private Dictionary<string, dynamic> _metadata;
        private bool disposedValue;

        public Dictionary<string, dynamic> Metadata
        {
            get { return _metadata; }
            set { _metadata = value; }
        }

        public MetricsManager(
                        string metricsNamespace = null,
                        string serviceName = null,
                        Dictionary<string, string> dimensions = null,
                        Dictionary<string, Metric> metrics = null,
                        Dictionary<string, dynamic> metadata = null)
        {
            _namespace = !string.IsNullOrEmpty(metricsNamespace) ? metricsNamespace : Environment.GetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE");
            _service = !string.IsNullOrEmpty(serviceName) ? serviceName : Environment.GetEnvironmentVariable("POWERTOOLS_SERVICE_NAME");
            _dimensions = dimensions != null ? dimensions : new Dictionary<string, string>();
            _metrics = metrics != null ? metrics : new Dictionary<string, Metric>();
            _metadata = metadata != null ? metadata : new Dictionary<string, dynamic>();

            if(!string.IsNullOrEmpty(_service)){
                _dimensions.Add("service", _service);
            }
        }

        public void SetNamespace(string metricsNamespace)
        {
            _namespace = metricsNamespace;
        }

        public void SetService(string metricsService)
        {
            _service = metricsService;
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
                Console.WriteLine($"Metric '{metric}' already exists. Skipping...");
                return;
            }

            _metrics.Add(metric.Name, metric);

            if (_metrics.Count == 100)
            {
                Flush();
                _metrics.Clear();
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
                            Namespace = Namespace,
                            Dimensions = new List<List<string>>{
                                extractDimensions(Dimensions)
                            },
                            Metrics = extractMetricDefinitionSet(Metrics)
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(metricObj, typeof(MetricsWrapper));

            string result = AddToJson(json, _dimensions, _metadata, _metrics);

            Console.WriteLine(result);

            _metrics.Clear();
        }

        private List<string> extractDimensions(Dictionary<string, string> dimensions)
        {
            List<string> result = new List<string>();

            result.AddRange(dimensions.Keys);

            return result;
        }

        private List<MetricsDefinitionSet> extractMetricDefinitionSet(Dictionary<string, Metric> metrics)
        {
            List<MetricsDefinitionSet> metricDefintionSet = new List<MetricsDefinitionSet>();

            foreach (var item in metrics)
            {
                metricDefintionSet.Add(new MetricsDefinitionSet
                {
                    Name = item.Value.Name,
                    Unit = item.Value.Unit
                });
            }

            return metricDefintionSet;
        }

        private string AddToJson(string json, Dictionary<string, string> dimensions, Dictionary<string, dynamic> metadata, Dictionary<string, Metric> metrics)
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
                result += $",\"{item.Name}\": {item.Value}";
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
                _namespace=null;
                _service=null;
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
