

using Amazon.Lambda.KinesisEvents;

namespace AWS.Lambda.Powertools.BatchProcessing.Kinesis;

/// <summary>
/// Fluent API builder for configuring and executing Kinesis batch processing with strongly-typed record handlers.
/// </summary>
public class TypedKinesisEventBatchProcessorBuilder : BatchProcessorBuilder<KinesisEvent, KinesisEvent.KinesisEventRecord>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypedKinesisEventBatchProcessorBuilder"/> class.
    /// </summary>
    /// <param name="batchProcessor">The underlying Kinesis batch processor to use for processing.</param>
    public TypedKinesisEventBatchProcessorBuilder(ITypedBatchProcessor<KinesisEvent, KinesisEvent.KinesisEventRecord> batchProcessor)
        : base(batchProcessor)
    {
    }

    /// <summary>
    /// Creates a new TypedKinesisEventBatchProcessorBuilder using the default TypedKinesisEventBatchProcessor instance.
    /// </summary>
    /// <returns>A new TypedKinesisEventBatchProcessorBuilder instance.</returns>
    public static TypedKinesisEventBatchProcessorBuilder Create()
    {
        return new TypedKinesisEventBatchProcessorBuilder(TypedKinesisEventBatchProcessor.TypedInstance);
    }

    /// <summary>
    /// Creates a new TypedKinesisEventBatchProcessorBuilder using the specified TypedKinesisEventBatchProcessor instance.
    /// </summary>
    /// <param name="batchProcessor">The TypedKinesisEventBatchProcessor instance to use.</param>
    /// <returns>A new TypedKinesisEventBatchProcessorBuilder instance.</returns>
    public static TypedKinesisEventBatchProcessorBuilder Create(TypedKinesisEventBatchProcessor batchProcessor)
    {
        return new TypedKinesisEventBatchProcessorBuilder(batchProcessor);
    }
}