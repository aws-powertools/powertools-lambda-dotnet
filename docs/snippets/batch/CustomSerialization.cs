// This file is referenced by docs/utilities/batch-processing.md
// via pymdownx.snippets (mkdocs).


using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using Docs.BatchProcessingSnippets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Docs.BatchSnippets.CustomSerialization.JsonSerializerContextConfiguration;

namespace Docs.BatchSnippets.CustomSerialization.JsonSerializerContextConfiguration
{
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
}


namespace Docs.BatchSnippets.CustomSerialization.JsonSerializerContextUsingWithAttribute
{
public class Function
{
// --8<-- [start:json_serializer_context_using_with_attribute]
[BatchProcessor(
    TypedRecordHandler = typeof(TypedSqsRecordHandler),
    JsonSerializerContext = typeof(MyJsonSerializerContext))]
public BatchItemFailuresResponse ProcessWithAot(SQSEvent sqsEvent)
{
    return TypedSqsBatchProcessor.Result.BatchItemFailuresResponse;
}
// --8<-- [end:json_serializer_context_using_with_attribute]
}
}
