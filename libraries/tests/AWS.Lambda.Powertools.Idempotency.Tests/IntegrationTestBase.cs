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
using AWS.Lambda.Powertools.Idempotency.Persistence;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests;

public class IntegrationTestBase : IAsyncLifetime
{
    protected const string TABLE_NAME = "idempotency_table";
    protected AmazonDynamoDBClient client;
    private protected DynamoDBPersistenceStore _dynamoDbPersistenceStore;

    public async Task InitializeAsync()
    {
        var credentials = new BasicAWSCredentials("FAKE", "FAKE");
        var amazonDynamoDbConfig = new AmazonDynamoDBConfig()
        {
            ServiceURL = new UriBuilder("http", "localhost", 8000).Uri.ToString(),
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
        try
        {
            await client.CreateTableAsync(createTableRequest);
            var response = await client.DescribeTableAsync(TABLE_NAME);
            if (response == null)
            {
                throw new NullReferenceException("Table was not created within the expected time");
            }
        }
        catch (ResourceInUseException e)
        {
            Console.WriteLine(e.Message);
        }
        
        _dynamoDbPersistenceStore = new DynamoDBPersistenceStoreBuilder()
            .WithTableName(TABLE_NAME)
            .WithDynamoDBClient(client)
            .Build();
        _dynamoDbPersistenceStore.Configure(new IdempotencyOptionsBuilder().Build(),functionName: null);
    }

    public Task DisposeAsync()
    {
        // Make sure delete item after each test
        _dynamoDbPersistenceStore.DeleteRecord("key").ConfigureAwait(false);
        //_dynamoContainer.DisposeAsync().ConfigureAwait(false);
        return Task.CompletedTask;
    }
}