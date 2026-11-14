// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).
//
// The region uses a file-scoped namespace, so it lives in its own file. The docs show the
// [assembly: LambdaSerializer] attribute, but it is compiled out: an assembly can only declare it once.
// Lines carrying a "hidden" snippet marker are compile-only and are not rendered in the docs.

using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Docs.Snippets.Kafka;

// --8<-- [start:class_library_deployment]
using AWS.Lambda.Powertools.Kafka;
using AWS.Lambda.Powertools.Kafka.Avro;
using AWS.Lambda.Powertools.Logging;

#if DOCS_ONLY // --8<-- [start:hidden]
[assembly: LambdaSerializer(typeof(PowertoolsKafkaAvroSerializer))] // Use PowertoolsKafkaAvroSerializer for Avro serialization
#endif // --8<-- [start:hidden]

namespace MyKafkaConsumer;

public class Function
{
    public string FunctionHandler(ConsumerRecords<string, CustomerProfile> records, ILambdaContext context)
    {
        foreach (var record in records)
        {
            Logger.LogInformation("Record Value: {@record}", record.Value);
        }

        return "Processed " + records.Count() + " records";
    }
}
// --8<-- [end:class_library_deployment]
