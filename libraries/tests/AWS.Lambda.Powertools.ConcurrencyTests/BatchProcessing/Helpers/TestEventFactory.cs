using System.Text;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;

namespace AWS.Lambda.Powertools.ConcurrencyTests.BatchProcessing.Helpers;

/// <summary>
/// Factory for creating test events for SQS, Kinesis, and DynamoDB batch processing tests.
/// </summary>
public static class TestEventFactory
{
    /// <summary>
    /// Creates an SQS event with the specified number of records.
    /// </summary>
    /// <param name="recordCount">Number of records to create.</param>
    /// <param name="invocationId">Unique identifier for the invocation (used in message bodies).</param>
    /// <returns>An SQSEvent with the specified records.</returns>
    public static SQSEvent CreateSqsEvent(int recordCount, string invocationId)
    {
        var records = new List<SQSEvent.SQSMessage>();
        for (int i = 0; i < recordCount; i++)
        {
            records.Add(new SQSEvent.SQSMessage
            {
                MessageId = $"{invocationId}-msg-{i}",
                Body = $"{{\"invocationId\":\"{invocationId}\",\"index\":{i}}}",
                ReceiptHandle = $"receipt-{invocationId}-{i}",
                EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
            });
        }

        return new SQSEvent { Records = records };
    }

    /// <summary>
    /// Creates an SQS FIFO event with the specified number of records.
    /// </summary>
    /// <param name="recordCount">Number of records to create.</param>
    /// <param name="invocationId">Unique identifier for the invocation.</param>
    /// <returns>An SQSEvent with FIFO queue source.</returns>
    public static SQSEvent CreateSqsFifoEvent(int recordCount, string invocationId)
    {
        var sqsEvent = CreateSqsEvent(recordCount, invocationId);
        foreach (var record in sqsEvent.Records)
        {
            record.EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue.fifo";
        }
        return sqsEvent;
    }

    /// <summary>
    /// Creates a Kinesis event with the specified number of records.
    /// </summary>
    /// <param name="recordCount">Number of records to create.</param>
    /// <param name="invocationId">Unique identifier for the invocation.</param>
    /// <returns>A KinesisEvent with the specified records.</returns>
    public static KinesisEvent CreateKinesisEvent(int recordCount, string invocationId)
    {
        var records = new List<KinesisEvent.KinesisEventRecord>();
        for (int i = 0; i < recordCount; i++)
        {
            var data = $"{{\"invocationId\":\"{invocationId}\",\"index\":{i}}}";
            records.Add(new KinesisEvent.KinesisEventRecord
            {
                EventId = $"{invocationId}-event-{i}",
                EventSourceARN = "arn:aws:kinesis:us-east-1:123456789012:stream/test-stream",
                Kinesis = new KinesisEvent.Record
                {
                    SequenceNumber = $"{invocationId}-seq-{i}",
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(data)),
                    PartitionKey = $"partition-{invocationId}"
                }
            });
        }

        return new KinesisEvent { Records = records };
    }

    /// <summary>
    /// Creates a DynamoDB Stream event with the specified number of records.
    /// </summary>
    /// <param name="recordCount">Number of records to create.</param>
    /// <param name="invocationId">Unique identifier for the invocation.</param>
    /// <returns>A DynamoDBEvent with the specified records.</returns>
    public static DynamoDBEvent CreateDynamoDbEvent(int recordCount, string invocationId)
    {
        var records = new List<DynamoDBEvent.DynamodbStreamRecord>();
        for (int i = 0; i < recordCount; i++)
        {
            records.Add(new DynamoDBEvent.DynamodbStreamRecord
            {
                EventID = $"{invocationId}-event-{i}",
                EventName = "INSERT",
                EventSourceArn = "arn:aws:dynamodb:us-east-1:123456789012:table/test-table/stream/2024-01-01T00:00:00.000",
                Dynamodb = new DynamoDBEvent.StreamRecord
                {
                    SequenceNumber = $"{invocationId}-seq-{i}",
                    StreamViewType = "NEW_AND_OLD_IMAGES",
                    Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                    {
                        ["pk"] = new DynamoDBEvent.AttributeValue { S = $"{invocationId}-pk-{i}" }
                    },
                    NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                    {
                        ["pk"] = new DynamoDBEvent.AttributeValue { S = $"{invocationId}-pk-{i}" },
                        ["data"] = new DynamoDBEvent.AttributeValue { S = $"{{\"invocationId\":\"{invocationId}\",\"index\":{i}}}" }
                    }
                }
            });
        }

        return new DynamoDBEvent { Records = records };
    }

    /// <summary>
    /// Gets all message IDs from an SQS event.
    /// </summary>
    public static HashSet<string> GetSqsMessageIds(SQSEvent sqsEvent)
    {
        return sqsEvent.Records.Select(r => r.MessageId).ToHashSet();
    }

    /// <summary>
    /// Gets all sequence numbers from a Kinesis event.
    /// </summary>
    public static HashSet<string> GetKinesisSequenceNumbers(KinesisEvent kinesisEvent)
    {
        return kinesisEvent.Records.Select(r => r.Kinesis.SequenceNumber).ToHashSet();
    }

    /// <summary>
    /// Gets all sequence numbers from a DynamoDB event.
    /// </summary>
    public static HashSet<string> GetDynamoDbSequenceNumbers(DynamoDBEvent dynamoDbEvent)
    {
        return dynamoDbEvent.Records.Select(r => r.Dynamodb.SequenceNumber).ToHashSet();
    }

    /// <summary>
    /// Creates an SQS event with typed message bodies that can be deserialized.
    /// </summary>
    /// <typeparam name="T">The type of message (must have InvocationId and Index properties).</typeparam>
    /// <param name="recordCount">Number of records to create.</param>
    /// <param name="invocationId">Unique identifier for the invocation.</param>
    /// <returns>An SQSEvent with typed JSON message bodies.</returns>
    public static SQSEvent CreateTypedSqsEvent<T>(int recordCount, string invocationId)
    {
        var records = new List<SQSEvent.SQSMessage>();
        for (int i = 0; i < recordCount; i++)
        {
            records.Add(new SQSEvent.SQSMessage
            {
                MessageId = $"{invocationId}-msg-{i}",
                Body = $"{{\"invocationId\":\"{invocationId}\",\"index\":{i},\"data\":\"test-data-{i}\"}}",
                ReceiptHandle = $"receipt-{invocationId}-{i}",
                EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
            });
        }

        return new SQSEvent { Records = records };
    }

    /// <summary>
    /// Creates an SQS event with a mix of valid and invalid JSON message bodies.
    /// </summary>
    /// <param name="recordCount">Total number of records to create.</param>
    /// <param name="invalidCount">Number of records with invalid JSON.</param>
    /// <param name="invocationId">Unique identifier for the invocation.</param>
    /// <returns>An SQSEvent with mixed valid/invalid message bodies.</returns>
    public static SQSEvent CreateMixedValidInvalidSqsEvent(int recordCount, int invalidCount, string invocationId)
    {
        var records = new List<SQSEvent.SQSMessage>();
        for (int i = 0; i < recordCount; i++)
        {
            string body;
            if (i < invalidCount)
            {
                // Invalid JSON that will cause deserialization to fail
                body = $"{{invalid-json-{invocationId}-{i}";
            }
            else
            {
                // Valid JSON
                body = $"{{\"invocationId\":\"{invocationId}\",\"index\":{i},\"data\":\"test-data-{i}\"}}";
            }
            
            records.Add(new SQSEvent.SQSMessage
            {
                MessageId = $"{invocationId}-msg-{i}",
                Body = body,
                ReceiptHandle = $"receipt-{invocationId}-{i}",
                EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
            });
        }

        return new SQSEvent { Records = records };
    }
}
