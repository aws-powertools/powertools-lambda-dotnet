using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;

namespace AOT_Function;

public static class TestHelperAot
{
    public static APIGatewayProxyResponse TestMethod(APIGatewayProxyRequest apigwProxyEvent)
    {
        var response = new Response
        {
            Greeting = "Hello Powertools for AWS Lambda (.NET)",
            Guid = Guid.NewGuid().ToString() // Guid generated in the Handler. used to compare Handler output
        };

        try
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(response, typeof(Response), LambdaFunctionJsonSerializerContext.Default),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception e)
        {
            return new APIGatewayProxyResponse
            {
                Body = e.Message,
                StatusCode = 500,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}

public class Response
{
    public string? Greeting { get; set; }
    public string? Guid { get; set; }
}