using Amazon.Lambda.APIGatewayEvents;

namespace AWS.Lambda.Powertools.Docs.Snippets.Idempotency;

public class Order { }
public class OrderResult { }
public class Response { }

public static class TestHelper
{
    public static APIGatewayProxyResponse TestMethod(APIGatewayProxyRequest request) => new();
}
