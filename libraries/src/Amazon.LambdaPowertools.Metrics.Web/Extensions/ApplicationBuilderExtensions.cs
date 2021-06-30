using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Amazon.LambdaPowertools.Metrics.Model;
using System.Diagnostics;

namespace Amazon.LambdaPowertools.Metrics.Web
{
    public static class ApplicationBuilderExtensions
    {
        private static bool _isColdStart = true;

        public static void UseMetricsMiddleware(this IApplicationBuilder app)
        {
            app.UseMetricsMiddleware((context, logger) =>
            {
                CaptureColdStart(logger);
                
                var endpoint = context.GetEndpoint();
                if(endpoint != null)
                {
                   var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();

                   logger.AddDimension("Controller", actionDescriptor.ControllerName);
                   logger.AddDimension("Action", actionDescriptor.ActionName);
                }

                // Include X-Ray trace if it is set
                var xRayTraceId = context.Request.Headers["X-Amzn-Trace-Id"];
                if(!string.IsNullOrEmpty(xRayTraceId) && xRayTraceId.Count > 0)
                {
                   logger.AddMetadata("XRayTraceId", xRayTraceId[0]);
                }

                // Include w3c trace id
                logger.AddMetadata("TraceId", Activity.Current?.Id ?? context?.TraceIdentifier);

                if (!string.IsNullOrEmpty(Activity.Current?.TraceStateString))
                {
                   logger.AddMetadata("TraceState", Activity.Current.TraceStateString);
                }

                logger.AddMetadata("Path", context.Request.Path);        
                
                return Task.CompletedTask;
            });
        }

        public static void UseMetricsMiddleware(this IApplicationBuilder app, Func<HttpContext, IMetricsLogger, Task> action)
        {
            app.Use(async (context, next) =>
            {
                await next.Invoke();
                
                var logger = context.RequestServices.GetRequiredService<IMetricsLogger>();

                await action(context, logger);
            });
        }

        private static void CaptureColdStart(IMetricsLogger logger)
        {
            if (_isColdStart)
            {
                string currentNamespace = logger.GetNamespace();

                logger.PushSingleMetric("ColdStart", 1, MetricsUnit.COUNT,metricsNamespace: currentNamespace);

                _isColdStart = false;
            }
        }
    }
}
