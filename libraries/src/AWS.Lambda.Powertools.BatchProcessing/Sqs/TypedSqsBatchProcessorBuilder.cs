

using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Sqs;

/// <summary>
/// Fluent API builder for configuring and executing SQS batch processing with strongly-typed record handlers.
/// </summary>
public class TypedSqsBatchProcessorBuilder : BatchProcessorBuilder<SQSEvent, SQSEvent.SQSMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypedSqsBatchProcessorBuilder"/> class.
    /// </summary>
    /// <param name="batchProcessor">The underlying SQS batch processor to use for processing.</param>
    public TypedSqsBatchProcessorBuilder(ITypedBatchProcessor<SQSEvent, SQSEvent.SQSMessage> batchProcessor)
        : base(batchProcessor)
    {
    }

    /// <summary>
    /// Creates a new TypedSqsBatchProcessorBuilder using the default TypedSqsBatchProcessor instance.
    /// </summary>
    /// <returns>A new TypedSqsBatchProcessorBuilder instance.</returns>
    public static TypedSqsBatchProcessorBuilder Create()
    {
        return new TypedSqsBatchProcessorBuilder(TypedSqsBatchProcessor.TypedInstance);
    }

    /// <summary>
    /// Creates a new TypedSqsBatchProcessorBuilder using the specified TypedSqsBatchProcessor instance.
    /// </summary>
    /// <param name="batchProcessor">The TypedSqsBatchProcessor instance to use.</param>
    /// <returns>A new TypedSqsBatchProcessorBuilder instance.</returns>
    public static TypedSqsBatchProcessorBuilder Create(TypedSqsBatchProcessor batchProcessor)
    {
        return new TypedSqsBatchProcessorBuilder(batchProcessor);
    }
}