// This file is referenced by docs/core/event_handler/bedrock_agent_function.md
// via pymdownx.snippets (mkdocs).

using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.EventHandler.Resolvers;

namespace AWS.Lambda.Powertools.Docs.Snippets.BedrockAgentFunction.ResponseFormat
{
internal class AirportService
{
    public AirportInfo GetAirportInfoForCity(string city) => new() { City = city, Code = "LHR", Name = "Heathrow" };
}

// --8<-- [start:response_format_tostring]
public class AirportInfo
{
    public string City { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Name} ({Code}) in {City}";
    }
}
// --8<-- [end:response_format_tostring]

internal class Example
{
    private readonly BedrockAgentFunctionResolver resolver = new();

    public void Register()
    {
// --8<-- [start:response_format_register]
resolver.Tool("getAirportCodeForCity", "Get airport code and full name for a specific city", (string city, ILambdaContext context) =>
{
    var airportService = new AirportService();
    var airportInfo = airportService.GetAirportInfoForCity(city);
    // Note: Best approach is to override the ToString method in the AirportInfo class
    return airportInfo;
});

//Alternatively, you can return an anonymous object if you dont override ToString()
// return new {
//     airportInfo
// };
// --8<-- [end:response_format_register]
    }
}
}
