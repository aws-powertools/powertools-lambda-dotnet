using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Common;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Internal;

public class BatchProcessingInternalTests
{
    [Fact]
    public void BatchProcessing_Set_Execution_Environment_Context_SQS()
    {
        // Arrange
        var env = new PowertoolsEnvironment();
        var conf = new PowertoolsConfigurations(env);
        
        // Act
        var sqsBatchProcessor = new SqsBatchProcessor(conf);

        // Assert
        Assert.Contains($"{Constants.FeatureContextIdentifier}/BatchProcessing/",
            env.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
        
        Assert.NotNull(sqsBatchProcessor);
    }
    
    [Fact]
    public void BatchProcessing_Set_Execution_Environment_Context_Kinesis()
    {
        // Arrange
        var env = new PowertoolsEnvironment();
        var conf = new PowertoolsConfigurations(env);
        
        // Act
        var KinesisEventBatchProcessor = new KinesisEventBatchProcessor(conf);

        // Assert
        Assert.Contains($"{Constants.FeatureContextIdentifier}/BatchProcessing/",
            env.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
        
        Assert.NotNull(KinesisEventBatchProcessor);
    }
    
    [Fact]
    public void BatchProcessing_Set_Execution_Environment_Context_DynamoDB()
    {
        // Arrange
        var env = new PowertoolsEnvironment();
        var conf = new PowertoolsConfigurations(env);
        
        // Act
        var dynamoDbStreamBatchProcessor = new DynamoDbStreamBatchProcessor(conf);

        // Assert
        Assert.Contains($"{Constants.FeatureContextIdentifier}/BatchProcessing/",
            env.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
        
        Assert.NotNull(dynamoDbStreamBatchProcessor);
    }
}