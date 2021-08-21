using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AWS.Lambda.PowerTools.Metrics.Web.Extensions;

namespace Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddMetrics();

            //
            // UNCOMMENT IF YOU WANT TO DEFINE THE NAMESPACE AND SERVICE NAME
            // CAN USE VALUES COMING FROM CONFIGURATION FILE OR HARCODED VALUES
            //
            //var metricsNamespace = $"{Configuration.GetSection("LambdaPowertools").GetValue<string>("POWERTOOLS_METRICS_NAMESPACE")}";
            //var serviceName = $"{Configuration.GetSection("LambdaPowertools").GetValue<string>("POWERTOOLS_SERVICE_NAME")}";

            //services.AddMetrics(metricsNamespace, serviceName);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseMetricsMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
