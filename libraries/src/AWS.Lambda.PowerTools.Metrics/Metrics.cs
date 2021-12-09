using System;
using System.Collections.Generic;
using AWS.Lambda.PowerTools.Core;

namespace AWS.Lambda.PowerTools.Metrics
{
    public class Metrics : IMetrics
    {
        private static IMetrics _instance;
        private readonly MetricsContext _context;
        private readonly bool _captureEmptyMetricsEnabled;
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        
        
        /// <summary>
        /// Creates Metrics  with no namespace or service name defined - requires that they are defined after initialization
        /// </summary>
       internal Metrics(IPowerToolsConfigurations powerToolsConfigurations, string metricsNamespace = null, string serviceName = null, bool captureMetricsEvenIfEmpty = false)
        {
            if (_instance != null) return;
            _instance = this;
            _powerToolsConfigurations = powerToolsConfigurations;
            _captureEmptyMetricsEnabled = captureMetricsEvenIfEmpty;
            _context = InitializeContext(metricsNamespace, serviceName, null);
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
                _instance.Flush(true);
            }

            _context.AddMetric(key, value, unit);
        }

        public static void AddMetric(string key, double value, MetricUnit unit = MetricUnit.NONE)
        {
            _instance.AddMetric(key, value, unit);
        }      

        void IMetrics.SetNamespace(string metricsNamespace)
        {
            _context.SetNamespace(metricsNamespace);
        }

        public static void SetNamespace(string metricsNamespace){
            _instance.SetNamespace(metricsNamespace);
        }

        string IMetrics.GetNamespace()
        {
            return _context.GetNamespace();
        }

        public static string GetNamespace(){
            return _instance.GetNamespace();
        }

        void IMetrics.AddDimension(string key, string value)
        {
            _context.AddDimension(new DimensionSet(key, value));
        }

        public static void AddDimension(string key, string value){
            _instance.AddDimension(key, value);
        }

        void IMetrics.AddMetadata(string key, dynamic value)
        {
            _context.AddMetadata(key, value);
        }

        public static void AddMetadata(string key, dynamic value){
            _instance.AddMetadata(key, value);
        }

        void IMetrics.SetDefaultDimensions(Dictionary<string, string> defaultDimensions){
            _context.SetDefaultDimensions(DictionaryToList(defaultDimensions));
        }

        public static void SetDefaultDimensions(Dictionary<string, string> defaultDimensions){
            _instance.SetDefaultDimensions(defaultDimensions);
        }

        void IMetrics.Flush(bool metricsOverflow)
        {
            if (_context.IsSerializable
                || _captureEmptyMetricsEnabled)
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
            _instance.PushSingleMetric(metricName, value, unit, metricsNamespace, serviceName, defaultDimensions);
        } 

        private MetricsContext InitializeContext(string metricsNamespace, string serviceName, Dictionary<string, string> defaultDimensions)
        {
            var context = new MetricsContext();

            if (!string.IsNullOrEmpty(metricsNamespace))
            {
                context.SetNamespace(metricsNamespace);
            }
            else if (!string.IsNullOrEmpty(_powerToolsConfigurations.MetricsNamespace))
            {
                context.SetNamespace(_powerToolsConfigurations.MetricsNamespace);
            }

            if (string.IsNullOrWhiteSpace(serviceName))
                serviceName = _powerToolsConfigurations.ServiceName;
            
            var defaultDimensionsList = DictionaryToList(defaultDimensions);

            // Add service as a default dimension
            defaultDimensionsList.Add(new DimensionSet("Service", serviceName));

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
            _instance.Flush();
        }

        internal static void ResetForTesting(){
            _instance = null;
        }
    }
}