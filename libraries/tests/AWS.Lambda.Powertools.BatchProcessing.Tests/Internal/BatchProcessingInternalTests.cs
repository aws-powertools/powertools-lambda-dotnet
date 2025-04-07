/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

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
        Assert.Equal($"{Constants.FeatureContextIdentifier}/BatchProcessing/1.0.0",
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
        Assert.Equal($"{Constants.FeatureContextIdentifier}/BatchProcessing/1.0.0",
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
        Assert.Equal($"{Constants.FeatureContextIdentifier}/BatchProcessing/1.0.0",
            env.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
        
        Assert.NotNull(dynamoDbStreamBatchProcessor);
    }
}