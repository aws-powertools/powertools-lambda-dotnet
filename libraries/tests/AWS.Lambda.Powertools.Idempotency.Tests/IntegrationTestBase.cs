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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests;

public class IntegrationTestBase: IAsyncLifetime
{
    protected const string TABLE_NAME = "idempotency_table";
    protected AmazonDynamoDBClient client;
    private TestcontainersContainer _testContainer;

    public virtual async Task InitializeAsync()
    {
        _testContainer = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("amazon/dynamodb-local:latest")
            .WithPortBinding(8000, assignRandomHostPort:true)
            .WithDockerEndpoint(Environment.GetEnvironmentVariable("DOCKER_HOST") ?? "unix:///var/run/docker.sock")
            .Build();
        await _testContainer.StartAsync();
        var credentials = new BasicAWSCredentials("FAKE", "FAKE");
        var amazonDynamoDbConfig = new AmazonDynamoDBConfig()
        {
            ServiceURL = $"http://localhost:{_testContainer.GetMappedPublicPort(8000)}",
            AuthenticationRegion = "us-east-1"
        };
        client = new AmazonDynamoDBClient(credentials, amazonDynamoDbConfig);

        var createTableRequest = new CreateTableRequest
        {
            TableName = TABLE_NAME,
            KeySchema = new List<KeySchemaElement>()
            {
                new("id", KeyType.HASH)
            },
            AttributeDefinitions = new List<AttributeDefinition>()
            {
                new()
                {
                    AttributeName = "id",
                    AttributeType = ScalarAttributeType.S
                }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        };
        await client.CreateTableAsync(createTableRequest);
        var response = await client.DescribeTableAsync(TABLE_NAME);
        if (response == null)
        {
            throw new NullReferenceException("Table was not created within the expected time");
        }
    }

    public virtual Task DisposeAsync()
    {
        return _testContainer.DisposeAsync().AsTask();
    }
}