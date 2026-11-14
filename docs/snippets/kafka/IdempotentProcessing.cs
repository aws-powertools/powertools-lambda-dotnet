// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).
//
// The region uses a file-scoped namespace, so it lives in its own file. The docs show the
// [assembly: LambdaSerializer] attribute, but it is compiled out: an assembly can only declare it once.
// Lines carrying a "hidden" snippet marker are compile-only and are not rendered in the docs.

using Docs.KafkaSnippets;

// --8<-- [start:idempotent_processing]
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Idempotency;

#if DOCS_ONLY // --8<-- [start:hidden]
[assembly: LambdaSerializer(typeof(PowertoolsKafkaProtobufSerializer))]
#endif // --8<-- [start:hidden]

namespace ProtoBufClassLibrary;

public class Function
{
    public Function()
    {
        Idempotency.Configure(builder => builder.UseDynamoDb("idempotency_table"));
    }

    public string FunctionHandler(ConsumerRecords<string, Payment> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            ProcessPayment(record.Value);
        }

        return "Processed " + records.Count() + " records";
    }

    [Idempotent]
    private string ProcessPayment(Payment payment)
    {
        Logger.LogInformation("Processing payment {paymentId} for customer {customerName}",
            payment.Id, payment.CustomerName);

        // Your payment processing logic here
        // This could involve calling an external payment service, updating a database, etc.

        return $"Payment {payment.Id} processed";
    }
}
// --8<-- [end:idempotent_processing]
