using System;
using Microsoft.Extensions.DependencyInjection;

namespace Amazon.LambdaPowertools.Metrics.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMetrics(this IServiceCollection services)
        {
            services.AddScoped<IMetricsLogger, MetricsLogger>();
        }
    }
}
