using Amazon.Lambda.APIGatewayEvents;

namespace Docs.IdempotencySnippets;

public class Order { }
public class OrderResult { }
public class Response { }

public static class TestHelper
{
    public static APIGatewayProxyResponse TestMethod(APIGatewayProxyRequest request) => new();
}
