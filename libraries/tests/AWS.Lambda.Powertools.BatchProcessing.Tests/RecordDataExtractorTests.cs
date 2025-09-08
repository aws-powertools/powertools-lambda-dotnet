

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

public class RecordDataExtractorTests
{
    #region SQS Record Data Extractor Tests

    [Fact]
    public void SqsRecordDataExtractor_ExtractData_ReturnsMessageBody()
    {
        // Arrange
        var extractor = SqsRecordDataExtractor.Instance;
        var messageBody = "{\"orderId\": \"12345\", \"amount\": 99.99}";
        var sqsMessage = new SQSEvent.SQSMessage
        {
            Body = messageBody,
            MessageId = "test-message-id"
        };

        // Act
        var result = extractor.ExtractData(sqsMessage);

        // Assert
        Assert.Equal(messageBody, result);
    }

    [Fact]
    public void SqsRecordDataExtractor_ExtractData_WithNullRecord_ReturnsEmptyString()
    {
        // Arrange
        var extractor = SqsRecordDataExtractor.Instance;

        // Act
        var result = extractor.ExtractData(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SqsRecordDataExtractor_ExtractData_WithNullBody_ReturnsEmptyString()
    {
        // Arrange
        var extractor = SqsRecordDataExtractor.Instance;
        var sqsMessage = new SQSEvent.SQSMessage
        {
            Body = null,
            MessageId = "test-message-id"
        };

        // Act
        var result = extractor.ExtractData(sqsMessage);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SqsRecordDataExtractor_Instance_IsSingleton()
    {
        // Arrange & Act
        var instance1 = SqsRecordDataExtractor.Instance;
        var instance2 = SqsRecordDataExtractor.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region Kinesis Record Data Extractor Tests

    [Fact]
    public void KinesisRecordDataExtractor_ExtractData_ReadsFromMemoryStream()
    {
        // Arrange
        var extractor = KinesisRecordDataExtractor.Instance;
        var originalData = "{\"userId\": \"user123\", \"action\": \"login\"}";
        var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(originalData));
        
        var kinesisRecord = new KinesisEvent.KinesisEventRecord
        {
            Kinesis = new KinesisEvent.Record
            {
                Data = dataStream,
                SequenceNumber = "12345"
            }
        };

        // Act
        var result = extractor.ExtractData(kinesisRecord);

        // Assert
        Assert.Equal(originalData, result);
    }

    [Fact]
    public void KinesisRecordDataExtractor_ExtractData_WithEmptyStream_ReturnsEmptyString()
    {
        // Arrange
        var extractor = KinesisRecordDataExtractor.Instance;
        var emptyStream = new MemoryStream();
        
        var kinesisRecord = new KinesisEvent.KinesisEventRecord
        {
            Kinesis = new KinesisEvent.Record
            {
                Data = emptyStream,
                SequenceNumber = "12345"
            }
        };

        // Act
        var result = extractor.ExtractData(kinesisRecord);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void KinesisRecordDataExtractor_ExtractData_WithNullRecord_ReturnsEmptyString()
    {
        // Arrange
        var extractor = KinesisRecordDataExtractor.Instance;

        // Act
        var result = extractor.ExtractData(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void KinesisRecordDataExtractor_ExtractData_WithNullKinesisData_ReturnsEmptyString()
    {
        // Arrange
        var extractor = KinesisRecordDataExtractor.Instance;
        var kinesisRecord = new KinesisEvent.KinesisEventRecord
        {
            Kinesis = new KinesisEvent.Record
            {
                Data = null,
                SequenceNumber = "12345"
            }
        };

        // Act
        var result = extractor.ExtractData(kinesisRecord);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void KinesisRecordDataExtractor_ExtractData_WithNullKinesis_ReturnsEmptyString()
    {
        // Arrange
        var extractor = KinesisRecordDataExtractor.Instance;
        var kinesisRecord = new KinesisEvent.KinesisEventRecord
        {
            Kinesis = null
        };

        // Act
        var result = extractor.ExtractData(kinesisRecord);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void KinesisRecordDataExtractor_Instance_IsSingleton()
    {
        // Arrange & Act
        var instance1 = KinesisRecordDataExtractor.Instance;
        var instance2 = KinesisRecordDataExtractor.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region DynamoDB Record Data Extractor Tests

    [Fact]
    public void DynamoDbRecordDataExtractor_ExtractData_SerializesDynamoDbRecord()
    {
        // Arrange
        var extractor = DynamoDbRecordDataExtractor.Instance;
        var dynamoDbRecord = new DynamoDBEvent.DynamodbStreamRecord
        {
            EventName = "INSERT",
            Dynamodb = new DynamoDBEvent.StreamRecord
            {
                Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["id"] = new DynamoDBEvent.AttributeValue { S = "123" }
                },
                NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["id"] = new DynamoDBEvent.AttributeValue { S = "123" },
                    ["name"] = new DynamoDBEvent.AttributeValue { S = "Test Item" }
                },
                SequenceNumber = "12345",
                SizeBytes = 100,
                StreamViewType = "NEW_AND_OLD_IMAGES"
            }
        };

        // Act
        var result = extractor.ExtractData(dynamoDbRecord);

        // Assert
        Assert.NotEmpty(result);
        
        // Verify the result is valid JSON
        var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.Equal("INSERT", deserializedResult.GetProperty("EventName").GetString());
        Assert.Equal("12345", deserializedResult.GetProperty("SequenceNumber").GetString());
        Assert.Equal(100, deserializedResult.GetProperty("SizeBytes").GetInt32());
    }

    [Fact]
    public void DynamoDbRecordDataExtractor_ExtractData_WithRemoveEvent_IncludesOldImage()
    {
        // Arrange
        var extractor = DynamoDbRecordDataExtractor.Instance;
        var dynamoDbRecord = new DynamoDBEvent.DynamodbStreamRecord
        {
            EventName = "REMOVE",
            Dynamodb = new DynamoDBEvent.StreamRecord
            {
                Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["id"] = new DynamoDBEvent.AttributeValue { S = "123" }
                },
                OldImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["id"] = new DynamoDBEvent.AttributeValue { S = "123" },
                    ["name"] = new DynamoDBEvent.AttributeValue { S = "Deleted Item" }
                },
                SequenceNumber = "12345",
                StreamViewType = "NEW_AND_OLD_IMAGES"
            }
        };

        // Act
        var result = extractor.ExtractData(dynamoDbRecord);

        // Assert
        Assert.NotEmpty(result);
        
        // Verify the result contains the old image
        var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.Equal("REMOVE", deserializedResult.GetProperty("EventName").GetString());
        Assert.True(deserializedResult.GetProperty("OldImage").ValueKind != JsonValueKind.Null);
    }

    [Fact]
    public void DynamoDbRecordDataExtractor_ExtractData_WithNullRecord_ReturnsEmptyString()
    {
        // Arrange
        var extractor = DynamoDbRecordDataExtractor.Instance;

        // Act
        var result = extractor.ExtractData(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void DynamoDbRecordDataExtractor_ExtractData_WithNullDynamoDb_ReturnsEmptyString()
    {
        // Arrange
        var extractor = DynamoDbRecordDataExtractor.Instance;
        var dynamoDbRecord = new DynamoDBEvent.DynamodbStreamRecord
        {
            EventName = "INSERT",
            Dynamodb = null
        };

        // Act
        var result = extractor.ExtractData(dynamoDbRecord);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void DynamoDbRecordDataExtractor_Instance_IsSingleton()
    {
        // Arrange & Act
        var instance1 = DynamoDbRecordDataExtractor.Instance;
        var instance2 = DynamoDbRecordDataExtractor.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion
}