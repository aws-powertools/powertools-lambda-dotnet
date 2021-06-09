using System;
using Amazon.LambdaPowertools.Metrics.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Amazon.LambdaPowertools.Metrics
{
    public class MetricsLogger : IMetricsLogger, IDisposable
    {
        private MetricsContext _context;
        private readonly ILogger _logger;
        private bool _isColdStart = true;

        public MetricsLogger() : this(NullLoggerFactory.Instance) { }
        

        public MetricsLogger(ILoggerFactory loggerFactory) : this(new MetricsContext(),null, null, loggerFactory) { }

        public MetricsLogger(string metricsNamespace, string metricsService) : this(new MetricsContext(), metricsNamespace, metricsService, null) { }

        public MetricsLogger(string metricsNamespace, string metricsService, ILoggerFactory loggerFactory) : this(new MetricsContext(), metricsNamespace, metricsService, loggerFactory) { }


        private MetricsLogger(MetricsContext metricsContext, string metricsNamespace, string metricsService, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory != null ? loggerFactory.CreateLogger<MetricsLogger>() : null;
            _context = metricsContext;

            CaptureColdStart();
            ConfigureContext(metricsNamespace, metricsService);
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

        public MetricsLogger AddDimension(DimensionSet dimension)
        {
            _context.AddDimension(dimension);
            return this;
        }

        public MetricsLogger SetDimensions(params DimensionSet[] dimensions)
        {
            _context.SetDimensions(dimensions);
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


        //TODO: Improve cold start capture process. Eventually with SingleMetric option
        private void CaptureColdStart()
        {
            if (_isColdStart)
            {
                _context.AddDimension("Cold Starts", PowertoolsConfig.Service);
                _context.AddMetric("ColdStart", 1, Unit.COUNT);

                Flush();

                _context = new MetricsContext();

                _isColdStart = false;
            }
        }

        private void ConfigureContext(string metricsNamespace, string metricsService)
        {
            PowertoolsConfig.SetNamespace(metricsNamespace);
            PowertoolsConfig.SetService(metricsService);

            _context.SetNamespace(PowertoolsConfig.Namespace);
            _context.AddDimension("Service", metricsService);
        }

        public void Dispose()
        {
            Flush();
        }
    }
}