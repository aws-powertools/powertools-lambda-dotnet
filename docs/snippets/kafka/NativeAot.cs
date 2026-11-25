// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).
//
// Each region is a top-level-statements Lambda function. To compile it inside the snippets library,
// it is wrapped in a namespace and a static method. The lines that open and close the wrapper
// carry a "hidden" snippet marker, so pymdownx.snippets drops them.

using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;

namespace Docs.KafkaSnippets.JsonNativeAot
{
// --8<-- [start:json_native_aot]
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Kafka.Json;

public static class Function { public static async Task RunAsync() { // --8<-- [start:hidden]
string Handler(ConsumerRecords<CustomerKey, CustomerProfile> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        context.Logger.LogInformation($"Customer {record.Key.Id}: {record.Value.FullName}");
    }

    return "Processed " + records.Count() + " records";
}

await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<CustomerKey, CustomerProfile>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaJsonSerializer(KafkaJsonContext.Default)) // Metadata for the key and value types
    .Build()
    .RunAsync();
} } // --8<-- [start:hidden]

[JsonSerializable(typeof(CustomerKey))]
[JsonSerializable(typeof(CustomerProfile))]
public partial class KafkaJsonContext : JsonSerializerContext
{
}
// --8<-- [end:json_native_aot]
}

namespace Docs.KafkaSnippets.AvroNativeAot
{
// --8<-- [start:avro_native_aot]
using System.Diagnostics.CodeAnalysis;
using AWS.Lambda.Powertools.Kafka.Avro;

public static class Function { public static async Task RunAsync() { // --8<-- [start:hidden]
string Handler(ConsumerRecords<string, Order> records, ILambdaContext context)
{
    foreach (var record in records)
    {
        context.Logger.LogInformation($"Order {record.Value.orderId} has {record.Value.lines.Count} lines");
    }

    return "Processed " + records.Count() + " records";
}

AvroCollections.Preserve();

await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<string, Order>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaAvroSerializer())
    .Build()
    .RunAsync();
} } // --8<-- [start:hidden]

// Apache.Avro creates the lists and dictionaries of array and map fields with reflection.
// Keep their constructors for every array (List<T>) and map (Dictionary<string, T>) in your schemas.
public static class AvroCollections
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(List<OrderLine>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(Dictionary<string, string>))]
    public static void Preserve()
    {
    }
}
// --8<-- [end:avro_native_aot]
}
