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
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Persistence;

// ReSharper disable once ClassNeverInstantiated.Global
public class DynamoDbFixture : IDisposable
{
    private readonly IContainer _container;
    public AmazonDynamoDBClient Client { get; set; }
    public string TableName { get; set; } = "idempotency_table";

    public DynamoDbFixture()
    {
        Environment.SetEnvironmentVariable("TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE","/var/run/docker.sock");
        
        _container = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("amazon/dynamodb-local:latest")
            .WithPortBinding(8000, true)
            .WithDockerEndpoint(Environment.GetEnvironmentVariable("DOCKER_HOST") ?? "unix:///var/run/docker.sock")
            .Build();
        
        
        _container.StartAsync().Wait();
        
        var credentials = new BasicAWSCredentials("FAKE", "FAKE");
        var amazonDynamoDbConfig = new AmazonDynamoDBConfig
        {
            ServiceURL = new UriBuilder("http", _container.Hostname, _container.GetMappedPublicPort(8000)).Uri.ToString(),
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
            Client.CreateTableAsync(createTableRequest).GetAwaiter().GetResult();
            var response = Client.DescribeTableAsync(TableName).GetAwaiter().GetResult();
            if (response == null)
            {
                throw new NullReferenceException("Table was not created within the expected time");
            }
        }
        catch (ResourceInUseException e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    public void Dispose()
    {
        _container.DisposeAsync().AsTask();
    }
}