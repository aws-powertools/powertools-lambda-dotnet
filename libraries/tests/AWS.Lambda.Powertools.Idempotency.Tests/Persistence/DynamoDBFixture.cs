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
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Testcontainers.DynamoDb;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Persistence;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DynamoDbFixture : IAsyncLifetime, IDisposable
{
    private readonly DynamoDbContainer _dynamoDbContainer = new DynamoDbBuilder().Build();

    public string TableName => "idempotency_table";

    public AmazonDynamoDBClient Client { get; set; }

    public async Task InitializeAsync()
    {
        await _dynamoDbContainer.StartAsync()
            .ConfigureAwait(false);

        var credentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");

        var config = new AmazonDynamoDBConfig();
        config.ServiceURL = _dynamoDbContainer.GetConnectionString();
        config.AuthenticationRegion = "us-east-1";

        var tableRequest = new CreateTableRequest();
        tableRequest.TableName = TableName;
        tableRequest.AttributeDefinitions = new List<AttributeDefinition> { new AttributeDefinition("id", ScalarAttributeType.S) };
        tableRequest.KeySchema = new List<KeySchemaElement> { new KeySchemaElement("id", KeyType.HASH) };
        tableRequest.BillingMode = BillingMode.PAY_PER_REQUEST;

        Client = new AmazonDynamoDBClient(credentials, config);

        try
        {
            await Client.CreateTableAsync(tableRequest)
                .ConfigureAwait(false);

            var tableResponse = await Client.DescribeTableAsync(TableName)
                .ConfigureAwait(false);

            if (tableResponse == null || !HttpStatusCode.OK.Equals(tableResponse.HttpStatusCode))
            {
                throw new TableNotFoundException($"{TableName} not found.");
            }
        }
        catch (ResourceInUseException e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task DisposeAsync()
    {
        await _dynamoDbContainer.DisposeAsync().AsTask()
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}