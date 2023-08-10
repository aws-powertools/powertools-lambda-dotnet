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

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.DynamoDB;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.DynamoDB;

public class DynamoDBProviderTest
{
    [Fact]
    public async Task GetAsync_SetupProvider_CallsHandler()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformerName = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetAsync<string>(
            key,
            Arg.Any<ParameterProviderConfiguration>(),
            null,
            null
        ).Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);
        provider.DefaultMaxAge(duration);
        provider.AddTransformer(transformerName, transformer);

        // Act
        var result = await provider.GetAsync(key);

        // Assert
        providerHandler.Received(1).SetCacheManager(cacheManager);
        providerHandler.Received(1).SetTransformerManager(transformerManager);
        providerHandler.Received(1).SetDefaultMaxAge(duration);
        providerHandler.Received(1).AddCustomTransformer(transformerName, transformer);
        await providerHandler.Received(1).GetAsync<string>(
            key,
            Arg.Any<ParameterProviderConfiguration>(),
            null,
            null
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenForceFetch_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Is<ParameterProviderConfiguration>(x => x != null && x.ForceFetch), null, null)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .ForceFetch()
            .GetAsync(key);

        // Assert
        await providerHandler
            .Received(1)
            .GetAsync<string>(key, Arg.Is<ParameterProviderConfiguration>(x => x != null && x.ForceFetch), null, null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithMaxAge_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Is<ParameterProviderConfiguration>(x => x != null && x.MaxAge == duration), null,
                null)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        await providerHandler
            .Received(1)
            .GetAsync<string>(key, Arg.Is<ParameterProviderConfiguration>(x => x != null && x.MaxAge == duration), null,
                null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformer_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key,
                Arg.Is<ParameterProviderConfiguration>(x => x != null && x.Transformer == transformer), null, null)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .WithTransformation(transformer)
            .GetAsync(key);

        // Assert
        await providerHandler
            .Received(1)
            .GetAsync<string>(key,
                Arg.Is<ParameterProviderConfiguration>(x => x != null && x.Transformer == transformer), null, null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformation_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Is<ParameterProviderConfiguration>(x => x != null && !x.ForceFetch),
                transformation, null)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .WithTransformation(transformation)
            .GetAsync(key);

        // Assert
        await providerHandler
            .Received(1)
            .GetAsync<string>(key,
                Arg.Is<ParameterProviderConfiguration>(x => x != null && !x.ForceFetch),
                transformation,
                null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformerName_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Is<ParameterProviderConfiguration>(x => x != null && !x.ForceFetch), null,
                transformerName)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .WithTransformation(transformerName)
            .GetAsync(key);

        // Assert
        await providerHandler
            .Received(1)
            .GetAsync<string>(key,
                Arg.Is<ParameterProviderConfiguration>(x => x != null && !x.ForceFetch),
                null,
                transformerName);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenCachedObjectExists_ReturnsCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var valueFromCache = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        cacheManager.Get(key).Returns(valueFromCache);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider.GetAsync(key);

        // Assert
        await client
            .DidNotReceive()
            .GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>());
        Assert.NotNull(result);
        Assert.Equal(valueFromCache, result);
    }
    
    [Fact]
    public async Task GetAsync_WhenForceFetch_IgnoresCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var response = new GetItemResponse
        {
            Item = new Dictionary<string, AttributeValue>()
            {
                { "value", new AttributeValue { S = value } }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key["id"].S == key),
            Arg.Any<CancellationToken>()
        ).Returns(response);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider.ForceFetch().GetAsync(key);

        // Assert
        cacheManager.DidNotReceive().Get(key);
        await client.Received(1).GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key["id"].S == key),
            Arg.Any<CancellationToken>()
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenMaxAgeNotSet_StoresCachedObjectWithDefaultMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge;
        var response = new GetItemResponse
        {
            Item = new Dictionary<string, AttributeValue>()
            {
                { "value", new AttributeValue { S = value } }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key["id"].S == key),
            Arg.Any<CancellationToken>()
        ).Returns(response);
        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider.GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key["id"].S == key),
            Arg.Any<CancellationToken>()
        );
        cacheManager.Received(1).Set(key, value, duration);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenMaxAgeClientSet_StoresCachedObjectWithDefaultMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var response = new GetItemResponse
        {
            Item = new Dictionary<string, AttributeValue>()
            {
                { "value", new AttributeValue { S = value } }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key["id"].S == key),
            Arg.Any<CancellationToken>()
        ).Returns(response);
        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(duration);

        // Act
        var result = await provider.GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key["id"].S == key),
            Arg.Any<CancellationToken>()
        );
        cacheManager.Received(1).Set(key, value, duration);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenMaxAgeSet_StoresCachedObjectWithMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var defaultMaxAge = CacheManager.DefaultMaxAge;
        var duration = defaultMaxAge.Add(TimeSpan.FromHours(10));
        var response = new GetItemResponse
        {
            Item = new Dictionary<string, AttributeValue>()
            {
                { "value", new AttributeValue { S = value } }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key["id"].S == key),
            Arg.Any<CancellationToken>()
        ).Returns(response);
        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await provider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key["id"].S == key),
            Arg.Any<CancellationToken>()
        );
        cacheManager.Received(1).Set(key, value, duration);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenTableNameSet_CallsClientWithTableName()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var tableName = Guid.NewGuid().ToString();
        var primaryKeyAttr = "id";
        var valueAttr = "value";
        var response = new GetItemResponse
        {
            Item = new Dictionary<string, AttributeValue>()
            {
                { valueAttr, new AttributeValue { S = value } }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key[primaryKeyAttr].S == key && x.TableName == tableName),
            Arg.Any<CancellationToken>()
        ).Returns(response);

        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .UseTable(tableName);

        // Act
        var result = await provider
            .GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key[primaryKeyAttr].S == key && x.TableName == tableName),
            Arg.Any<CancellationToken>()
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenTableInfoSet_CallsClientWithTableInfo()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var tableName = Guid.NewGuid().ToString();
        var primaryKeyAttr = Guid.NewGuid().ToString();
        var valueAttr = Guid.NewGuid().ToString();
        var response = new GetItemResponse
        {
            Item = new Dictionary<string, AttributeValue>()
            {
                { valueAttr, new AttributeValue { S = value } }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key[primaryKeyAttr].S == key && x.TableName == tableName),
            Arg.Any<CancellationToken>()
        ).Returns(response);

        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .UseTable(tableName, primaryKeyAttr, valueAttr);

        // Act
        var result = await provider
            .GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetItemAsync(
            Arg.Is<GetItemRequest>(x => x.Key[primaryKeyAttr].S == key && x.TableName == tableName),
            Arg.Any<CancellationToken>()
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_SetupProvider_CallsHandler()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var transformerName = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);
        provider.DefaultMaxAge(duration);
        provider.AddTransformer(transformerName, transformer);

        // Act
        var result = await provider.GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key, null, null, null);
        providerHandler.Received(1).SetCacheManager(cacheManager);
        providerHandler.Received(1).SetTransformerManager(transformerManager);
        providerHandler.Received(1).SetDefaultMaxAge(duration);
        providerHandler.Received(1).AddCustomTransformer(transformerName, transformer);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }
    
    [Fact]
    public async Task GetMultipleAsync_WhenForceFetch_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .ForceFetch()
            .GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.ForceFetch),
            null, null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WithMaxAge_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .WithMaxAge(duration)
            .GetMultipleAsync(key);
        
        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.MaxAge == duration),
            null, null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }
    
    [Fact]
    public async Task GetMultipleAsync_WithTransformer_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .WithTransformation(transformer)
            .GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == transformer),
            null, null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WithTransformation_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), transformation, null)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .WithTransformation(transformation)
            .GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x =>
                x != null && !x.ForceFetch),
            Arg.Is<Transformation?>(x => x == transformation),
            null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WithTransformerName_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, transformerName)
            .Returns(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler);
        provider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider
            .WithTransformation(transformerName)
            .GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x =>
                x != null && !x.ForceFetch),
            null,
            Arg.Is<string?>(x => x == transformerName));
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }
    
    [Fact]
    public async Task GetMultipleAsync_WhenCachedObjectExists_ReturnsCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var valueFromCache = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        cacheManager.Get(key).Returns(valueFromCache);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider.GetMultipleAsync(key);

        // Assert
        await client.DidNotReceiveWithAnyArgs().QueryAsync(default);
        Assert.NotNull(result);
        Assert.Equal(valueFromCache, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenForceFetch_IgnoresCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var valueFromCache = new Dictionary<string, string>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var value = new Dictionary<string, string>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var response = new QueryResponse
        {
            Items = value.Select(kv =>
                new Dictionary<string, AttributeValue>
                {
                    { "sk", new AttributeValue { S = kv.Key } },
                    { "value", new AttributeValue { S = kv.Value } }
                }).ToList()
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(valueFromCache);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider.ForceFetch().GetMultipleAsync(key);

        // Assert
        cacheManager.DidNotReceive().Get(key);
        await client.Received(1).QueryAsync(
            Arg.Is<QueryRequest>(x =>
                x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                    .Split('=', StringSplitOptions.TrimEntries).Last() &&
                x.ExpressionAttributeValues.First().Value.S == key
            ),
            Arg.Any<CancellationToken>()
        );
        Assert.NotNull(result);
        Assert.Equal(value.First().Key, result.First().Key);
        Assert.Equal(value.First().Value, result.First().Value);
        Assert.Equal(value.Last().Key, result.Last().Key);
        Assert.Equal(value.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenMaxAgeNotSet_StoresCachedObjectWithDefaultMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge;
        var value = new Dictionary<string, string>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var response = new QueryResponse
        {
            Items = value.Select(kv =>
                new Dictionary<string, AttributeValue>
                {
                    { "sk", new AttributeValue { S = kv.Key } },
                    { "value", new AttributeValue { S = kv.Value } }
                }).ToList()
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await provider.GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).QueryAsync(
            Arg.Is<QueryRequest>(x =>
                x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                    .Split('=', StringSplitOptions.TrimEntries).Last() &&
                x.ExpressionAttributeValues.First().Value.S == key
            ),
            Arg.Any<CancellationToken>()
        );
        cacheManager.Received(1).Set(key, Arg.Is<Dictionary<string, string>>(x =>
            x.First().Key == result.First().Key &&
            x.First().Value == result.First().Value &&
            x.Last().Key == result.Last().Key &&
            x.Last().Value == result.Last().Value
        ), duration);

        Assert.NotNull(result);
        Assert.Equal(value.First().Key, result.First().Key);
        Assert.Equal(value.First().Value, result.First().Value);
        Assert.Equal(value.Last().Key, result.Last().Key);
        Assert.Equal(value.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenMaxAgeClientSet_StoresCachedObjectWithDefaultMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var value = new Dictionary<string, string>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var response = new QueryResponse
        {
            Items = value.Select(kv =>
                new Dictionary<string, AttributeValue>
                {
                    { "sk", new AttributeValue { S = kv.Key } },
                    { "value", new AttributeValue { S = kv.Value } }
                }).ToList()
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(duration);

        // Act
        var result = await provider.GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).QueryAsync(
            Arg.Is<QueryRequest>(x =>
                x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                    .Split('=', StringSplitOptions.TrimEntries).Last() &&
                x.ExpressionAttributeValues.First().Value.S == key
            ),
            Arg.Any<CancellationToken>()
        );
        cacheManager.Received(1).Set(key, Arg.Is<Dictionary<string, string>>(x =>
            x.First().Key == result.First().Key &&
            x.First().Value == result.First().Value &&
            x.Last().Key == result.Last().Key &&
            x.Last().Value == result.Last().Value
        ), duration);

        Assert.NotNull(result);
        Assert.Equal(value.First().Key, result.First().Key);
        Assert.Equal(value.First().Value, result.First().Value);
        Assert.Equal(value.Last().Key, result.Last().Key);
        Assert.Equal(value.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenMaxAgeSet_StoresCachedObjectWithMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var defaultMaxAge = CacheManager.DefaultMaxAge;
        var duration = defaultMaxAge.Add(TimeSpan.FromHours(10));
        var value = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var response = new QueryResponse
        {
            Items = value.Select(kv =>
                new Dictionary<string, AttributeValue>()
                {
                    { "sk", new AttributeValue { S = kv.Key } },
                    { "value", new AttributeValue { S = kv.Value } }
                }).ToList()
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await provider
            .WithMaxAge(duration)
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).QueryAsync(
            Arg.Is<QueryRequest>(x =>
                x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                    .Split('=', StringSplitOptions.TrimEntries).Last() &&
                x.ExpressionAttributeValues.First().Value.S == key
            ),
            Arg.Any<CancellationToken>()
        );
        cacheManager.Received(1).Set(key, Arg.Is<Dictionary<string, string>>(x =>
            x.First().Key == result.First().Key &&
            x.First().Value == result.First().Value &&
            x.Last().Key == result.Last().Key &&
            x.Last().Value == result.Last().Value
        ), duration);

        Assert.NotNull(result);
        Assert.Equal(value.First().Key, result.First().Key);
        Assert.Equal(value.First().Value, result.First().Value);
        Assert.Equal(value.Last().Key, result.Last().Key);
        Assert.Equal(value.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenTableNameSet_CallsClientWithTableName()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var tableName = Guid.NewGuid().ToString();
        var primaryKeyAttribute = "id";
        var sortKeyAttribute = "sk";
        var valueAttribute = "value";
        var value = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var response = new QueryResponse
        {
            Items = value.Select(kv =>
                new Dictionary<string, AttributeValue>()
                {
                    { sortKeyAttribute, new AttributeValue { S = kv.Key } },
                    { valueAttribute, new AttributeValue { S = kv.Value } }
                }).ToList()
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .UseTable(tableName);

        // Act
        var result = await provider
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).QueryAsync(
            Arg.Is<QueryRequest>(x =>
                x.TableName == tableName &&
                x.KeyConditionExpression.Split('=', StringSplitOptions.TrimEntries).First() ==
                primaryKeyAttribute &&
                x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                    .Split('=', StringSplitOptions.TrimEntries).Last() &&
                x.ExpressionAttributeValues.First().Value.S == key
            ),
            Arg.Any<CancellationToken>()
        );


        Assert.NotNull(result);
        Assert.Equal(value.First().Key, result.First().Key);
        Assert.Equal(value.First().Value, result.First().Value);
        Assert.Equal(value.Last().Key, result.Last().Key);
        Assert.Equal(value.Last().Value, result.Last().Value);
    }
    
    [Fact]
    public async Task GetMultipleAsync_WhenTableInfoSet_CallsClientWithTableInfo()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var tableName = Guid.NewGuid().ToString();
        var primaryKeyAttribute = Guid.NewGuid().ToString();
        var sortKeyAttribute = Guid.NewGuid().ToString();
        var valueAttribute = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var response = new QueryResponse
        {
            Items = value.Select(kv =>
                new Dictionary<string, AttributeValue>()
                {
                    { sortKeyAttribute, new AttributeValue { S = kv.Key } },
                    { valueAttribute, new AttributeValue { S = kv.Value } }
                }).ToList()
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonDynamoDB>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .UseTable(tableName, primaryKeyAttribute, sortKeyAttribute, valueAttribute);

        // Act
        var result = await provider
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).QueryAsync(
            Arg.Is<QueryRequest>(x =>
                x.TableName == tableName &&
                x.KeyConditionExpression.Split('=', StringSplitOptions.TrimEntries).First() ==
                primaryKeyAttribute &&
                x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                    .Split('=', StringSplitOptions.TrimEntries).Last() &&
                x.ExpressionAttributeValues.First().Value.S == key
            ),
            Arg.Any<CancellationToken>()
        );

        Assert.NotNull(result);
        Assert.Equal(value.First().Key, result.First().Key);
        Assert.Equal(value.First().Value, result.First().Value);
        Assert.Equal(value.Last().Key, result.Last().Key);
        Assert.Equal(value.Last().Value, result.Last().Value);
    }
}