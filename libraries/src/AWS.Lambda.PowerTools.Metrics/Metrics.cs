using System;
using System.Collections.Generic;

namespace AWS.Lambda.PowerTools.Metrics
{
    public class Metrics : IMetrics
    {
        private static IMetrics Instance { get; set; }

        private string MetricsNamespace { get; set; }
        private string ServiceName { get; set; }
        private bool CaptureEmptyMetricsEnabled { get; set; }
        private MetricsContext _context;

        /// <summary>
        /// Creates Metrics  with no namespace or service name defined - requires that they are defined after initialization
        /// </summary>
       public Metrics(string metricsNamespace = null, string serviceName = null, bool captureMetricsEvenIfEmpty = false)
        {
            if (Instance == null)
            {
                MetricsNamespace = metricsNamespace;
                ServiceName = serviceName;
                CaptureEmptyMetricsEnabled = captureMetricsEvenIfEmpty;

                Instance = this;
                _context = InitializeContext(MetricsNamespace, ServiceName, null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        void IMetrics.AddMetric(string key, double value, MetricUnit unit)
        {
            if (_context.GetMetrics().Count == 100)
            {
                Instance.Flush(true);
            }

            _context.AddMetric(key, value, unit);
        }

        public static void AddMetric(string key, double value, MetricUnit unit = MetricUnit.NONE)
        {
            Instance.AddMetric(key, value, unit);
        }      

        void IMetrics.SetNamespace(string metricsNamespace)
        {
            _context.SetNamespace(metricsNamespace);
        }

        public static void SetNamespace(string metricsNamespace){
            Instance.SetNamespace(metricsNamespace);
        }

        string IMetrics.GetNamespace()
        {
            return _context.GetNamespace();
        }

        public static string GetNamespace(){
            return Instance.GetNamespace();
        }

        void IMetrics.AddDimension(string key, string value)
        {
            _context.AddDimension(new DimensionSet(key, value));
        }

        public static void AddDimension(string key, string value){
            Instance.AddDimension(key, value);
        }

        void IMetrics.AddMetadata(string key, dynamic value)
        {
            _context.AddMetadata(key, value);
        }

        public static void AddMetadata(string key, dynamic value){
            Instance.AddMetadata(key, value);
        }

        void IMetrics.SetDefaultDimensions(Dictionary<string, string> defaultDimensions){
            _context.SetDefaultDimensions(DictionaryToList(defaultDimensions));
        }

        public static void SetDefaultDimensions(Dictionary<string, string> defaultDimensions){
            Instance.SetDefaultDimensions(defaultDimensions);
        }

        public static void Flush(){
            Instance.Flush(false);
        }

        void IMetrics.Flush(bool metricsOverflow)
        {
            if (_context.IsSerializable
                || CaptureEmptyMetricsEnabled)
            {
                var emfPayload = _context.Serialize();

                Console.WriteLine(emfPayload);

                _context.ClearMetrics();

                if (!metricsOverflow) { _context.ClearNonDefaultDimensions(); }
            }
            else
            {
                Console.WriteLine("##WARNING## Metrics and Metadata have not been specified. No data will be sent to Cloudwatch Metrics.");
            }
        }

        private void Flush(MetricsContext context)
        {
            var emfPayload = context.Serialize();

            Console.WriteLine(emfPayload);
        }

        public string Serialize()
        {
            return _context.Serialize();
        }

        void IMetrics.PushSingleMetric(string metricName, double value, MetricUnit unit, string metricsNamespace, string serviceName, Dictionary<string, string> defaultDimensions)
        {
            using var context = InitializeContext(metricsNamespace, serviceName, defaultDimensions);
            context.AddMetric(metricName, value, unit);

            Flush(context);
        }

        public static void PushSingleMetric(string metricName, double value, MetricUnit unit, string metricsNamespace = null, string serviceName = null, Dictionary<string, string> defaultDimensions = null){
            Instance.PushSingleMetric(metricName, value, unit, metricsNamespace, serviceName, defaultDimensions);
        } 

        private MetricsContext InitializeContext(string metricsNamespace, string serviceName, Dictionary<string, string> defaultDimensions)
        {
            var context = new MetricsContext();

            if (!string.IsNullOrEmpty(metricsNamespace))
            {
                context.SetNamespace(metricsNamespace);
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE")))
            {
                context.SetNamespace(Environment.GetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE"));
            }

            PowertoolsConfig.Service = serviceName;

            var defaultDimensionsList = DictionaryToList(defaultDimensions);

            // Add service as a default dimension
            defaultDimensionsList.Add(new DimensionSet("Service", PowertoolsConfig.Service));

            context.SetDefaultDimensions(defaultDimensionsList);

            return context;
        }

        private List<DimensionSet> DictionaryToList(Dictionary<string, string> defaultDimensions)
        {
            List<DimensionSet> defaultDimensionsList = new List<DimensionSet>();
            if (defaultDimensions != null)
            {
                foreach (var item in defaultDimensions)
                {
                    defaultDimensionsList.Add(new DimensionSet(item.Key, item.Value));
                }
            }

            return defaultDimensionsList;
        }

        public void Dispose()
        {
            Flush();
        }

        internal static void ResetForTesting(){
            Instance = null;
        }
    }
}