using AWS.Lambda.Powertools.Tracing;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing.UsingStatementPattern.basic_using_statement
{
using Tracing = global::AWS.Lambda.Powertools.Tracing.Tracing;
// --8<-- [start:basic_using_statement]
using AWS.Lambda.Powertools.Tracing;

public partial class Function
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
    return null;
    }
}
// --8<-- [end:basic_using_statement]
public partial class Function
{
    private Task<PaymentResult> ProcessPaymentAsync() => Task.FromResult(new PaymentResult());
}

}

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing.UsingStatementPattern.custom_namespace
{
using Tracing = global::AWS.Lambda.Powertools.Tracing.Tracing;
// --8<-- [start:custom_namespace]
using AWS.Lambda.Powertools.Tracing;

public partial class Function
{
    public async Task<APIGatewayProxyResponse> FunctionHandler
        (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        using var segment = Tracing.BeginSubsegment("MyCustomNamespace", "DatabaseOperation");
        segment.AddAnnotation("TableName", "Users");
        segment.AddMetadata("query", "SELECT * FROM Users WHERE Active = 1");
    return null;
    }
}
// --8<-- [end:custom_namespace]
}

namespace AWS.Lambda.Powertools.Docs.Snippets.Tracing.UsingStatementPattern.nested_subsegments
{
using Tracing = global::AWS.Lambda.Powertools.Tracing.Tracing;
// --8<-- [start:nested_subsegments]
using AWS.Lambda.Powertools.Tracing;

public partial class Function
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
    return null;
    }
}
// --8<-- [end:nested_subsegments]
public partial class Function
{
    private Task<PaymentResult> ProcessPaymentAsync() => Task.FromResult(new PaymentResult());
    private Task PostProcessPaymentAsync(PaymentResult result) => Task.CompletedTask;
}

}

internal class PaymentResult
{
    public int ProcessingTimeMs { get; set; }
    public string PaymentId { get; set; }
}
