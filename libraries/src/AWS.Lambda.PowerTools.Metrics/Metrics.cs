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
        /// Creates a Metrics object that provides features to send metrics to Amazon Cloudwatch using the Embedded metric format (EMF). See https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html 
        /// </summary>
        /// <param name="powerToolsConfigurations"></param>
        /// <param name="metricsNamespace"></param>
        /// <param name="serviceName"></param>
        /// <param name="captureMetricsEvenIfEmpty"></param>
       internal Metrics(IPowerToolsConfigurations powerToolsConfigurations, string metricsNamespace = null, string serviceName = null, bool captureMetricsEvenIfEmpty = false)
        {
            if (_instance != null) return;

            _instance = this;
            _powerToolsConfigurations = powerToolsConfigurations;
            _captureEmptyMetricsEnabled = captureMetricsEvenIfEmpty;
            _context = InitializeContext(metricsNamespace, serviceName, null);
        }

        /// <summary>
        /// Implements interface that adds new metric to memory.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        void IMetrics.AddMetric(string key, double value, MetricUnit unit)
        {
            if(string.IsNullOrWhiteSpace(key)){
                throw new ArgumentException("'AddMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.");
            }

            if (_context.GetMetrics().Count == 100)
            {
                _instance.Flush(true);
            }

            _context.AddMetric(key, value, unit);
        }

        /// <summary>
        /// Adds new metric to memory.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="unit"></param>
        public static void AddMetric(string key, double value, MetricUnit unit = MetricUnit.NONE)
        {
            _instance.AddMetric(key, value, unit);
        }      

        /// <summary>
        /// Implements interface that sets metrics namespace identifier.
        /// </summary>
        /// <param name="metricsNamespace"></param>
        void IMetrics.SetNamespace(string metricsNamespace)
        {
            _context.SetNamespace(metricsNamespace);
        }

        /// <summary>
        /// Sets metrics namespace identifier.
        /// </summary>
        /// <param name="metricsNamespace"></param>
        public static void SetNamespace(string metricsNamespace){
            _instance.SetNamespace(metricsNamespace);
        }

        /// <summary>
        /// Implements interface that allows retrieval of namespace identifier.
        /// </summary>
        /// <returns>Namespace identifier</returns>
        string IMetrics.GetNamespace()
        {
            return _context.GetNamespace();
        }

        /// <summary>
        /// Retrieves namespace identifier.
        /// </summary>
        /// <returns>Namespace identifier</returns>
        public static string GetNamespace(){
            return _instance.GetNamespace();
        }

        /// <summary>
        /// Implements interface that adds a dimension.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void IMetrics.AddDimension(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("'AddDimension' method requires a valid dimension key. 'Null' or empty values are not allowed.");
            }

            _context.AddDimension(new DimensionSet(key, value));
        }

        public static void AddDimension(string key, string value){
            _instance.AddDimension(key, value);
        }

        void IMetrics.AddMetadata(string key, dynamic value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("'AddMetadata' method requires a valid metadata key. 'Null' or empty values are not allowed.");
            }

            _context.AddMetadata(key, value);
        }

        public static void AddMetadata(string key, dynamic value){
            _instance.AddMetadata(key, value);
        }

        void IMetrics.SetDefaultDimensions(Dictionary<string, string> defaultDimensions){
            foreach (var item in defaultDimensions)
            {
                if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                {
                    throw new ArgumentException("'SetDefaultDimensions' method requires a valid key pair. 'Null' or empty values are not allowed.");
                }
            }

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
            if (string.IsNullOrWhiteSpace(metricName))
            {
                throw new ArgumentException("'AddMetric' method requires a valid metrics key. 'Null' or empty values are not allowed.");
            }

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

            if (!string.IsNullOrWhiteSpace(metricsNamespace))
            {
                context.SetNamespace(metricsNamespace);
            }
            else if (!string.IsNullOrWhiteSpace(_powerToolsConfigurations.MetricsNamespace))
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