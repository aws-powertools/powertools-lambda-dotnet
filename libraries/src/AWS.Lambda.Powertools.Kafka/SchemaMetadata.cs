namespace AWS.Lambda.Powertools.Kafka;

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