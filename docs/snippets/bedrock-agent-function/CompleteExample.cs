// This file is referenced by docs/core/event_handler/bedrock_agent_function.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.BedrockAgentFunction;

// --8<-- [start:complete_example_with_di]
using Amazon.BedrockAgentRuntime.Model;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MyBedrockAgent
{
    // Service interfaces and implementations
    public interface IWeatherService
    {
        string GetForecast(string city);
    }

    public class WeatherService : IWeatherService
    {
        public string GetForecast(string city) => $"Weather forecast for {city}: Sunny, 75°F";
    }

    public interface IProductService
    {
        string CheckInventory(string productId);
    }

    public class ProductService : IProductService
    {
        public string CheckInventory(string productId) => $"Product {productId} has 25 units in stock";
    }

    // Main Lambda function
    public class Function
    {
        private readonly BedrockAgentFunctionResolver _resolver;

        public Function()
        {
            // Set up dependency injection
            var services = new ServiceCollection();
            services.AddSingleton<IWeatherService, WeatherService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddBedrockResolver(); // Extension method to register the resolver

            var serviceProvider = services.BuildServiceProvider();
            _resolver = serviceProvider.GetRequiredService<BedrockAgentFunctionResolver>();

            // Register tool functions that use injected services
            _resolver
                .Tool("GetWeatherForecast",
                    "Gets weather forecast for a city",
                    (string city, IWeatherService weatherService, ILambdaContext ctx) =>
                    {
                        ctx.Logger.LogLine($"Weather request for {city}");
                        return weatherService.GetForecast(city);
                    })
                .Tool("CheckInventory",
                    "Checks inventory for a product",
                    (string productId, IProductService productService) =>
                        productService.CheckInventory(productId))
                .Tool("GetServerTime",
                    "Returns the current server time",
                    () => DateTime.Now.ToString("F"));
        }

        public ActionGroupInvocationOutput FunctionHandler(
            ActionGroupInvocationInput input, ILambdaContext context)
        {
            return _resolver.Resolve(input, context);
        }
    }
}
// --8<-- [end:complete_example_with_di]
