using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.PowerTools.Metrics.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMetrics(this IServiceCollection services)
        {
            services.AddScoped<IMetrics>(ctx => new Metrics(false));
        }

        public static void AddMetrics(this IServiceCollection services, string metricsNamespace, string serviceName)
        {
            services.AddScoped<IMetrics>(ctx => new Metrics(metricsNamespace, serviceName, false));
        }
    }
}
