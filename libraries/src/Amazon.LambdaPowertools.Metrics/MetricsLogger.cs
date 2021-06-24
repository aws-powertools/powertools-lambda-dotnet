using System;
using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{
    public class MetricsLogger : IMetricsLogger
    {
        private MetricsContext _context;
        private bool _isColdStart = true;        

        /// <summary>
        /// Creates Metrics Logger with no namespace or service name defined - requires that they are defined after initialization
        /// </summary>
        public MetricsLogger() : this(new MetricsContext(),null, null, true) { }

        public MetricsLogger(bool captureColdStart) : this(new MetricsContext(), null, null, captureColdStart) {}

        public MetricsLogger(string metricsNamespace, string metricsService) : this(new MetricsContext(), metricsNamespace, metricsService, true) { }

        public MetricsLogger(string metricsNamespace, string metricsService, bool captureColdStart) : this(new MetricsContext(), metricsNamespace, metricsService, captureColdStart) { }

        private MetricsLogger(MetricsContext metricsContext, string metricsNamespace, string metricsService, bool captureColdStart)
        {
            _context = metricsContext;

            ConfigureContext(metricsNamespace, metricsService);

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
        public MetricsLogger AddMetric(string key, double value, Unit unit = Unit.NONE)
        {
            if(_context.GetMetrics().Count == 100)
            {
                Flush();
            }

            _context.AddMetric(key, value, unit);

            return this;
        }

        public MetricsLogger SetNamespace(string metricNamespace)
        {
            _context.SetNamespace(metricNamespace);
            return this;
        }

        public MetricsLogger AddDimension(string key, string value)
        {
            _context.AddDimension(new DimensionSet(key, value));
            return this;
        }

        public MetricsLogger AddMetadata(string key, dynamic value)
        {
            _context.AddMetadata(key, value);
            return this;
        }

        public void Flush()
        {
            var EMFPayload = _context.Serialize();

            Console.WriteLine(EMFPayload);

            _context.ClearMetrics();
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
                _context.AddMetric("ColdStart", 1, Unit.COUNT);

                Flush();

                _context.ClearMetrics();

                _isColdStart = false;
            }
        }

        public void PushSingleMetric(string metricName, double value, Unit unit, string metricsNamespace = null, string serviceName = null){
            using(var context = new MetricsContext()){

                ConfigureContext(metricsNamespace, serviceName);

                context.AddMetric(metricName, value, unit);

                Flush(context);
            }            
        }

        private void ConfigureContext(string metricsNamespace, string metricsService)
        {
            if (!string.IsNullOrEmpty(metricsNamespace))
            {
                _context.SetNamespace(metricsNamespace);
            }
            else
            {
                // TODO Add support for lambda environment variables and .net core config variables
            }

            if (!string.IsNullOrEmpty(metricsService))
            {
                _context.AddDimension("ServiceName", metricsService);
            }
            else
            {
                // TODO Add support for lambda environment variables and .net core config variables               
            }
        }

        public void Dispose()
        {
            Flush();
        }
    }
}