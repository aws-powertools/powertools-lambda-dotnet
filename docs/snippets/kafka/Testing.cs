// This file is referenced by docs/utilities/kafka.md
// via pymdownx.snippets (mkdocs).

using Xunit;

namespace Docs.KafkaSnippets;

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

        // Build a Kafka event like the one Lambda sends: keys and values are base64-encoded
        var product = new ProtobufProduct { Name = "Test Product", Id = 1, Price = 99.99 };
        var key = Convert.ToBase64String(Encoding.UTF8.GetBytes("42"));
        var value = Convert.ToBase64String(product.ToByteArray());

        var kafkaEvent = $$"""
            {
                "eventSource": "aws:kafka",
                "records": {
                    "mytopic-0": [
                        {
                            "topic": "mytopic",
                            "partition": 0,
                            "offset": 15,
                            "timestamp": 1545084650987,
                            "timestampType": "CREATE_TIME",
                            "key": "{{key}}",
                            "value": "{{value}}",
                            "headers": []
                        }
                    ]
                }
            }
            """;

        // Deserialize the event with the same serializer the Lambda runtime uses
        var serializer = new PowertoolsKafkaProtobufSerializer();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEvent));
        var records = serializer.Deserialize<ConsumerRecords<int, ProtobufProduct>>(stream);

        // Call the handler
        var result = Handler(records, mockContext);

        // Assert the result
        Assert.Equal("Successfully processed Protobuf Kafka events", result);

        // Verify the context logger output
        Assert.Contains("Processing Test Product at $99.99", mockLogger.Buffer.ToString());

        // Verify the records were processed
        var processed = Assert.Single(records);
        Assert.Equal("mytopic", processed.Topic);
        Assert.Equal(0, processed.Partition);
        Assert.Equal(15, processed.Offset);
        Assert.Equal(42, processed.Key);
        Assert.Equal("Test Product", processed.Value.Name);
        Assert.Equal(1, processed.Value.Id);
        Assert.Equal(99.99, processed.Value.Price);
    }
}
// --8<-- [end:testing_your_code]
