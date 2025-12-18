// This file is referenced by docs/utilities/batch-processing.md
// via pymdownx.snippets (mkdocs).

// --8<-- [start:json_serializer_context_configuration]
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(List<Product>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class MyJsonSerializerContext : JsonSerializerContext
{
}
// --8<-- [end:json_serializer_context_configuration]

// --8<-- [start:json_serializer_context_using_with_attribute]
[BatchProcessor(
    TypedRecordHandler = typeof(TypedSqsRecordHandler),
    JsonSerializerContext = typeof(MyJsonSerializerContext))]
public BatchItemFailuresResponse ProcessWithAot(SQSEvent sqsEvent)
{
    return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:json_serializer_context_using_with_attribute]
