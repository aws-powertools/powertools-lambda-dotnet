// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Kafka;

// --8<-- [start:testing_your_code]
using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Kafka.Protobuf;
using Google.Protobuf;
using TestKafka;

public class KafkaTests
{
    [Fact]
    public void SimpleHandlerTest()
    {
        string Handler(ConsumerRecords<int, ProtobufProduct> records, ILambdaContext context)
        {
            foreach (var record in records)
            {
                var product = record.Value;
                context.Logger.LogInformation($"Processing {product.Name} at ${product.Price}");
            }

            return "Successfully processed Protobuf Kafka events";
        }
        // Simulate the handler execution
        var mockLogger = new TestLambdaLogger();
        var mockContext = new TestLambdaContext
        {
            Logger = mockLogger
        };

        var records = new ConsumerRecords<int, ProtobufProduct>
        {
            Records = new Dictionary<string, List<ConsumerRecord<int, ProtobufProduct>>>
            {
                { "mytopic-0", new List<ConsumerRecord<int, ProtobufProduct>>
                    {
                        new()
                        {
                            Topic = "mytopic",
                            Partition = 0,
                            Offset = 15,
                            Key = 42,
                            Value = new ProtobufProduct { Name = "Test Product", Id = 1, Price = 99.99 }
                        }
                    }
                }
            }
        };

        // Call the handler
        var result = Handler(records, mockContext);

        // Assert the result
        Assert.Equal("Successfully processed Protobuf Kafka events", result);

        // Verify the context logger output
        Assert.Contains("Processing Test Product at $99.99", mockLogger.Buffer.ToString());

        // Verify the records were processed
        Assert.Single(records.Records);
        Assert.Contains("mytopic-0", records.Records.Keys);
        Assert.Single(records.Records["mytopic-0"]);
        Assert.Equal("mytopic", records.Records["mytopic-0"][0].Topic);
        Assert.Equal(0, records.Records["mytopic-0"][0].Partition);
        Assert.Equal(15, records.Records["mytopic-0"][0].Offset);
        Assert.Equal(42, records.Records["mytopic-0"][0].Key);
        Assert.Equal("Test Product", records.Records["mytopic-0"][0].Value.Name);
        Assert.Equal(1, records.Records["mytopic-0"][0].Value.Id);
        Assert.Equal(99.99, records.Records["mytopic-0"][0].Value.Price);
    }
}
// --8<-- [end:testing_your_code]
