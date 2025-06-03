using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.EventHandler.Resolvers;
using AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Models;
using AWS.Lambda.Powertools.Logging;
using BedrockAgentFunction;
using Microsoft.Extensions.Logging;


var logger = LoggerFactory.Create(builder =>
{
    builder.AddPowertoolsLogger(config => { config.Service = "AirportService"; });
}).CreatePowertoolsLogger();

var resolver = new BedrockAgentFunctionResolver();


resolver.Tool("getAirportCodeForCity", "Get airport code and full name for a specific city", (string city, ILambdaContext context) =>
{
    logger.LogInformation($"Getting airport code for city: {city}");
    var airportService = new AirportService();
    var airportInfo = airportService.GetAirportInfoForCity(city);

    logger.LogInformation($"Airport for {city}: {airportInfo.Code} - {airportInfo.Name}");
    
    // Note: Best approach is to override the ToString method in the AirportInfo class
    // public override string ToString()
    // {
    //     return $"{Name} ({Code}) in {City}";
    // }
    // This will return a string with properties Code and Name
    return airportInfo;
    
    //Alternatively, you can return an anonymous object
    // return new {
    //     airportInfo
    // }; 
});


// The function handler that will be called for each Lambda event
var handler = async (BedrockFunctionRequest input, ILambdaContext context) =>
{
    return await resolver.ResolveAsync(input, context);
};

// Build the Lambda runtime client passing in the handler to call for each
// event and the JSON serializer to use for translating Lambda JSON documents
// to .NET types.
await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
    .Build()
    .RunAsync();
    
    
