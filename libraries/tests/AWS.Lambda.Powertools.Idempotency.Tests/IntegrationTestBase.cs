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
    protected const string TableName = "idempotency_table";
    protected AmazonDynamoDBClient Client;
    private protected DynamoDBPersistenceStore DynamoDbPersistenceStore;

    public async Task InitializeAsync()
    {
        // initialize TestContainers or Ductus.FluentDocker or have DynamoDb local docker running
        
        var credentials = new BasicAWSCredentials("FAKE", "FAKE");
        var amazonDynamoDbConfig = new AmazonDynamoDBConfig
        {
            ServiceURL = new UriBuilder("http", "localhost", 8000).Uri.ToString(),
            AuthenticationRegion = "us-east-1"
        };
        Client = new AmazonDynamoDBClient(credentials, amazonDynamoDbConfig);

        var createTableRequest = new CreateTableRequest
        {
            TableName = TableName,
            KeySchema = new List<KeySchemaElement>
            {
                new("id", KeyType.HASH)
            },
            AttributeDefinitions = new List<AttributeDefinition>
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
            await Client.CreateTableAsync(createTableRequest);
            var response = await Client.DescribeTableAsync(TableName);
            if (response == null)
            {
                throw new NullReferenceException("Table was not created within the expected time");
            }
        }
        catch (ResourceInUseException e)
        {
            Console.WriteLine(e.Message);
        }
        
        DynamoDbPersistenceStore = new DynamoDBPersistenceStoreBuilder()
            .WithTableName(TableName)
            .WithDynamoDBClient(Client)
            .Build();
        DynamoDbPersistenceStore.Configure(new IdempotencyOptionsBuilder().Build(),functionName: null);
    }

    public Task DisposeAsync()
    {
        // Make sure delete item after each test
        DynamoDbPersistenceStore.DeleteRecord("key").ConfigureAwait(false);
        //_dynamoContainer.DisposeAsync().ConfigureAwait(false);
        return Task.CompletedTask;
    }
}