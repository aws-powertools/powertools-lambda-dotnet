// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing;

// --8<-- [start:basic_using_statement]
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        using var gatewaySegment = Tracing.BeginSubsegment("PaymentGatewayIntegration");
        gatewaySegment.AddAnnotation("Operation", "ProcessPayment");
        gatewaySegment.AddAnnotation("PaymentMethod", "CreditCard");

        var result = await ProcessPaymentAsync();
        gatewaySegment.AddAnnotation("ProcessingTimeMs", result.ProcessingTimeMs);
        // Subsegment automatically ends when disposed
    }
}
// --8<-- [end:basic_using_statement]

// --8<-- [start:custom_namespace]
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        using var segment = Tracing.BeginSubsegment("MyCustomNamespace", "DatabaseOperation");
        segment.AddAnnotation("TableName", "Users");
        segment.AddMetadata("query", "SELECT * FROM Users WHERE Active = 1");
    }
}
// --8<-- [end:custom_namespace]

// --8<-- [start:nested_subsegments]
using AWS.Lambda.Powertools.Tracing;

public class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        using var outerSegment = Tracing.BeginSubsegment("PaymentProcessing");
        outerSegment.AddAnnotation("Operation", "ProcessPayment");

        var result = await ProcessPaymentAsync();

        using var postProcessingSegment = Tracing.BeginSubsegment("PaymentPostProcessing");
        postProcessingSegment.AddAnnotation("PaymentId", result.PaymentId);

        await PostProcessPaymentAsync(result);
    }
}
// --8<-- [end:nested_subsegments]
