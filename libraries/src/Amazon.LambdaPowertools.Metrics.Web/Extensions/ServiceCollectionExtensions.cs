using Microsoft.Extensions.DependencyInjection;

namespace Amazon.LambdaPowertools.Metrics.Web
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMetrics(this IServiceCollection services)
        {
            services.AddScoped<IMetricsLogger>(ctx => new MetricsLogger(false));
        }

        public static void AddMetrics(this IServiceCollection services, string metricsNamespace, string metricsServiceName)
        {
            services.AddScoped<IMetricsLogger>(ctx => new MetricsLogger(metricsNamespace, metricsServiceName, false));
        }
    }
}
