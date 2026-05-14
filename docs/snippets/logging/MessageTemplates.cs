// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Logging;

// --8<-- [start:tostring_override]
public class User
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }

    public override string ToString()
    {
        return $"{LastName}, {FirstName} ({Age})";
    }
}
// --8<-- [end:tostring_override]

// --8<-- [start:message_template_at]
public class Function
{
    [Logging(Service = "user-service", LogLevel = LogLevel.Information)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 42
        };

        logger.LogInformation("User object: {@user}", user);
        ...
    }
}
// --8<-- [end:message_template_at]

// --8<-- [start:message_template_tostring]
public class Function
{
    [Logging(Service = "user", LogLevel = LogLevel.Information)]
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 42
        };

        logger.LogInformation("User data: {user}", user);

        // Also works with numbers, dates, etc.

        logger.LogInformation("Price: {price:0.00}", 123.4567); // will respect decimal places
        logger.LogInformation("Percentage: {percent:0.0%}", 0.1234);
        ...
    }
}
// --8<-- [end:message_template_tostring]
