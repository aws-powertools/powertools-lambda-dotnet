using System;
using AWS.Lambda.PowerTools.Metrics.Model;

namespace AWS.Lambda.PowerTools.Metrics
{
    public class Metrics : IMetrics
    {
        private MetricsContext _context;
        private bool _isColdStart = true;  
        private bool _captureMetricsEvenIfEmpty;      
        
        private static Metrics _instance; 
        public static Metrics Instance => _instance ??= new Metrics("dotnet-lambdapowertools", "lambda-example");

        /// <summary>
        /// Creates Metrics  with no namespace or service name defined - requires that they are defined after initialization
        /// </summary>
        public Metrics() : this(new MetricsContext(),null, null, true, false) { }

        public Metrics(bool captureColdStart) : this(new MetricsContext(), null, null, captureColdStart, false) {}

        public Metrics(string metricsNamespace, string serviceName) : this(new MetricsContext(), metricsNamespace, serviceName, true, false) { }

        
        public Metrics(string metricsNamespace, string serviceName, bool captureColdStart) : this(new MetricsContext(), metricsNamespace, serviceName, captureColdStart, false) { }

        private Metrics(MetricsContext metricsContext, string metricsNamespace, string serviceName, bool captureColdStart, bool captureMetricsEvenIfEmpty)
        {
            _context = metricsContext;
            _captureMetricsEvenIfEmpty = captureMetricsEvenIfEmpty;

            ConfigureContext(in _context, metricsNamespace, serviceName);

            if(captureColdStart){
                CaptureColdStart();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public Metrics AddMetric(string key, double value, MetricUnit unit = MetricUnit.NONE)
        {
            if(_context.GetMetrics().Count == 100)
            {
                Flush();
            }

            _context.AddMetric(key, value, unit);

            return this;
        }

        public Metrics SetNamespace(string metricsNamespace)
        {
            _context.SetNamespace(metricsNamespace);

            return this;
        }

        public string GetNamespace()
        {
            return _context.GetNamespace();
        }

        public Metrics AddDimension(string key, string value)
        {
            _context.AddDimension(new DimensionSet(key, value));
            return this;
        }

        public Metrics AddMetadata(string key, dynamic value)
        {
            _context.AddMetadata(key, value);
            return this;
        }

        public void Flush()
        {
            if(_context.IsSerializable 
                || _captureMetricsEvenIfEmpty){
                var EMFPayload = _context.Serialize();

                Console.WriteLine(EMFPayload);

                _context.ClearMetrics();
            }
            else {
                Console.WriteLine("##WARNING## Metrics and Metadata have not been specified. No data will be sent to Cloudwatch Metrics.");
            }
        }

        private void Flush(MetricsContext context)
        {
            var EMFPayload = context.Serialize();

            Console.WriteLine(EMFPayload);
        }

        public string Serialize()
        {
            try
            {
                return _context.Serialize();
            }
            catch (ArgumentException)
            {
                throw;
            }
        }

        
        private void CaptureColdStart()
        {
            if (_isColdStart)
            {
                _context.AddMetric("ColdStart", 1, MetricUnit.COUNT);

                Flush();

                _context.ClearMetrics();

                _isColdStart = false;
            }
        }

        public void PushSingleMetric(string metricName, double value, MetricUnit unit, string metricsNamespace = null, string serviceName = null){
            using(var context = new MetricsContext()){

                ConfigureContext(in context, metricsNamespace, serviceName);

                context.AddMetric(metricName, value, unit);

                Flush(context);
            }            
        }

        private void ConfigureContext(in MetricsContext context, string metricsNamespace, string serviceName)
        {
            if (!string.IsNullOrEmpty(metricsNamespace))
            {
                context.SetNamespace(metricsNamespace);
            }
            else if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE")))
            {
                context.SetNamespace(Environment.GetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE"));
            }
            
            PowertoolsConfig.Service = serviceName;
            context.AddDimension("Service", PowertoolsConfig.Service);
        }

        public void Dispose()
        {
            Flush();
        }
    }
}