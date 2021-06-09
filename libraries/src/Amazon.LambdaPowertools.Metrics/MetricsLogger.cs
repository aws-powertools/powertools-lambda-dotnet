using System;
using Amazon.LambdaPowertools.Metrics.Model;

namespace Amazon.LambdaPowertools.Metrics
{
    public class MetricsLogger : IMetricsLogger
    {
        private MetricsContext _context;
        private bool _isColdStart = true;        

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
                if(!string.IsNullOrEmpty(metricsNamespace)){
                    context.SetNamespace(metricsNamespace);
                }
                else{
                    context.SetNamespace(PowertoolsConfig.Namespace);
                }

                if (!string.IsNullOrEmpty(serviceName))
                {
                    context.AddDimension("ServiceName", serviceName);
                }
                else
                {
                    context.AddDimension("ServiceName", PowertoolsConfig.Service);
                }

                context.AddMetric(metricName, value, unit);

                Flush(context);
            }            
        }

        private void ConfigureContext(string metricsNamespace, string metricsService)
        {
            if (!string.IsNullOrEmpty(metricsNamespace))
            {
                PowertoolsConfig.SetNamespace(metricsNamespace);
            }

            if (!string.IsNullOrEmpty(metricsService))
            {
                PowertoolsConfig.SetService(metricsService);
            }

            _context.SetNamespace(PowertoolsConfig.Namespace);
            _context.AddDimension("ServiceName", PowertoolsConfig.Service);
        }

        public void Dispose()
        {
            Flush();
        }
    }
}