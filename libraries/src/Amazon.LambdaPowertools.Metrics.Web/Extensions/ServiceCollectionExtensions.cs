using Microsoft.Extensions.DependencyInjection;

namespace Amazon.LambdaPowertools.Metrics.Web
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMetrics(this IServiceCollection services)
        {
            services.AddScoped<IMetricsLogger>(ctx => new MetricsLogger(false));
        }

        public static void AddMetrics(this IServiceCollection services, string metricsNamespace, string serviceName)
        {
            services.AddScoped<IMetricsLogger>(ctx => new MetricsLogger(metricsNamespace, serviceName, false));
        }
    }
}
