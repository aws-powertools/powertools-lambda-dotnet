using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.PowerTools.Metrics.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMetrics(this IServiceCollection services, bool captureColdStart = false)
        {
            services.AddScoped<IMetrics>(ctx => new Metrics(captureColdStart));
        }

        public static void AddMetrics(this IServiceCollection services, string metricsNamespace, string serviceName, bool captureColdStart = false)
        {
            services.AddScoped<IMetrics>(ctx => new Metrics(metricsNamespace, serviceName, captureColdStart));
        }
    }
}
