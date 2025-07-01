#if KAFKA_JSON
namespace AWS.Lambda.Powertools.Kafka.Json;
#elif KAFKA_AVRO
namespace AWS.Lambda.Powertools.Kafka.Avro;
#elif KAFKA_PROTOBUF
namespace AWS.Lambda.Powertools.Kafka.Protobuf;
#else
namespace AWS.Lambda.Powertools.Kafka;
#endif

/// <summary>
/// Represents metadata about the schema used for serializing the record's value or key.
/// </summary>
public class SchemaMetadata
{
    /// <summary>
    /// Gets or sets the format of the data (e.g., "JSON", "AVRO" "Protobuf").
    /// /// </summary>
    public string DataFormat { get; internal set; } = null!;
    
    /// <summary>
    /// Gets or sets the schema ID associated with the record's value or key.
    /// </summary>
    public string SchemaId { get; internal set; } = null!;
}