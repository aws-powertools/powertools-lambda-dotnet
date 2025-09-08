

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Interface for extracting data from event records for deserialization.
/// </summary>
/// <typeparam name="TRecord">The type of the event record.</typeparam>
public interface IRecordDataExtractor<in TRecord>
{
    /// <summary>
    /// Extracts the data string from the event record that should be deserialized.
    /// </summary>
    /// <param name="record">The event record to extract data from.</param>
    /// <returns>The data string to be deserialized.</returns>
    string ExtractData(TRecord record);
}