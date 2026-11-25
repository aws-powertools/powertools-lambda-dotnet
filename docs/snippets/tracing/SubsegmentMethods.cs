using AWS.Lambda.Powertools.Tracing;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// This file is referenced by docs/core/tracing.md
// via pymdownx.snippets (mkdocs).

namespace Docs.TracingSnippets.SubsegmentMethods.available_methods
{
public class Function
{
    public void Handler()
    {
        object paymentObject = new();
        string requestId = "request-id";
        Exception exception = new();
// --8<-- [start:available_methods]
using var segment = Tracing.BeginSubsegment("PaymentProcessing");

// Add annotations (indexed by X-Ray)
segment.AddAnnotation("PaymentMethod", "CreditCard");
segment.AddAnnotation("Amount", 99.99);

// Add metadata (not indexed, for additional context)
segment.AddMetadata("PaymentDetails", paymentObject);
segment.AddMetadata("CustomNamespace", "RequestId", requestId);

// Add exception information
segment.AddException(exception);

// Add HTTP information
segment.AddHttpInformation("response_code", 200);
segment.AddHttpInformation("url", "https://api.payment.com/process");
// --8<-- [end:available_methods]
    }
}
}
