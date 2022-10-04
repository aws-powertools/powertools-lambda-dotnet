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
using Moq;
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);
        provider.DefaultMaxAge(duration);
        provider.AddTransformer(transformerName, transformer.Object);

        // Act
        var result = await provider.GetAsync(key);

        // Assert
        providerHandler.Verify(v => v.GetAsync<string>(key, null, null, null), Times.Once);
        providerHandler.Verify(v => v.SetCacheManager(cacheManager.Object), Times.Once);
        providerHandler.Verify(v => v.SetTransformerManager(transformerManager.Object), Times.Once);
        providerHandler.Verify(v => v.SetDefaultMaxAge(duration), Times.Once);
        providerHandler.Verify(v => v.AddCustomTransformer(transformerName, transformer.Object), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenForceFetch_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .ForceFetch()
            .GetAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetAsync<string>(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && x.ForceFetch
                ), null,
                null), Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetAsync<string>(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && x.MaxAge == duration
                ), null,
                null), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformer_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .WithTransformation(transformer.Object)
            .GetAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetAsync<string>(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && x.Transformer == transformer.Object
                ), null,
                null), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformation_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), transformation, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .WithTransformation(transformation)
            .GetAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetAsync<string>(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && !x.ForceFetch
                ),
                It.Is<Transformation?>(x => x == transformation),
                null), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformerName_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, transformerName)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .WithTransformation(transformerName)
            .GetAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetAsync<string>(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && !x.ForceFetch
                ),
                null,
                It.Is<string?>(x => x == transformerName))
            , Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenCachedObjectExists_ReturnsCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var valueFromCache = Guid.NewGuid().ToString();
        var response = new GetItemResponse
        {
            Item = new Dictionary<string, AttributeValue>()
            {
                { "value", new AttributeValue { S = value } }
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider.GetAsync(key);

        // Assert
        client.Verify(v =>
                v.GetItemAsync(It.IsAny<GetItemRequest>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.NotNull(result);
        Assert.Equal(valueFromCache, result);
    }

    [Fact]
    public async Task GetAsync_WhenForceFetch_IgnoresCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var valueFromCache = Guid.NewGuid().ToString();
        var response = new GetItemResponse
        {
            Item = new Dictionary<string, AttributeValue>()
            {
                { "value", new AttributeValue { S = value } }
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider.ForceFetch().GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Never);
        client.Verify(v =>
                v.GetItemAsync(
                    It.Is<GetItemRequest>(x =>
                        x.Key["id"].S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider.GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetItemAsync(
                    It.Is<GetItemRequest>(x =>
                        x.Key["id"].S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(duration);

        // Act
        var result = await provider.GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetItemAsync(
                    It.Is<GetItemRequest>(x =>
                        x.Key["id"].S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await provider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetItemAsync(
                    It.Is<GetItemRequest>(x =>
                        x.Key["id"].S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .UseTable(tableName);

        // Act
        var result = await provider
            .GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetItemAsync(
                    It.Is<GetItemRequest>(x =>
                        x.Key[primaryKeyAttr].S == key && x.TableName == tableName
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .UseTable(tableName, primaryKeyAttr, valueAttr);

        // Act
        var result = await provider
            .GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetItemAsync(
                    It.Is<GetItemRequest>(x =>
                        x.Key[primaryKeyAttr].S == key && x.TableName == tableName
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_SetupProvider_CallsHandler()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var transformerName = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);
        provider.DefaultMaxAge(duration);
        provider.AddTransformer(transformerName, transformer.Object);

        // Act
        var result = await provider.GetMultipleAsync(key);

        // Assert
        providerHandler.Verify(v => v.GetMultipleAsync(key, null, null, null), Times.Once);
        providerHandler.Verify(v => v.SetCacheManager(cacheManager.Object), Times.Once);
        providerHandler.Verify(v => v.SetTransformerManager(transformerManager.Object), Times.Once);
        providerHandler.Verify(v => v.SetDefaultMaxAge(duration), Times.Once);
        providerHandler.Verify(v => v.AddCustomTransformer(transformerName, transformer.Object), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenForceFetch_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .ForceFetch()
            .GetMultipleAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetMultipleAsync(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && x.ForceFetch
                ), null,
                null), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WithMaxAge_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .WithMaxAge(duration)
            .GetMultipleAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetMultipleAsync(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && x.MaxAge == duration
                ), null,
                null), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WithTransformer_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .WithTransformation(transformer.Object)
            .GetMultipleAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetMultipleAsync(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && x.Transformer == transformer.Object
                ), null,
                null), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WithTransformation_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), transformation, null)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .WithTransformation(transformation)
            .GetMultipleAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetMultipleAsync(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && !x.ForceFetch
                ),
                It.Is<Transformation?>(x => x == transformation),
                null), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WithTransformerName_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, transformerName)
        ).ReturnsAsync(value);

        var provider = new DynamoDBProvider();
        provider.SetHandler(providerHandler.Object);
        provider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider
            .WithTransformation(transformerName)
            .GetMultipleAsync(key);

        // Assert
        providerHandler.Verify(
            v => v.GetMultipleAsync(key,
                It.Is<ParameterProviderConfiguration?>(x =>
                    x != null && !x.ForceFetch
                ),
                null,
                It.Is<string?>(x => x == transformerName))
            , Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenCachedObjectExists_ReturnsCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var valueFromCache = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider.GetMultipleAsync(key);

        // Assert
        client.Verify(v =>
                v.QueryAsync(It.IsAny<QueryRequest>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.NotNull(result);
        Assert.Equal(valueFromCache, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenForceFetch_IgnoresCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var valueFromCache = new Dictionary<string, string>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider.ForceFetch().GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Never);
        client.Verify(v =>
                v.QueryAsync(
                    It.Is<QueryRequest>(x =>
                        x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                            .Split('=', StringSplitOptions.TrimEntries).Last() &&
                        x.ExpressionAttributeValues.First().Value.S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await provider.GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.QueryAsync(
                    It.Is<QueryRequest>(x =>
                        x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                            .Split('=', StringSplitOptions.TrimEntries).Last() &&
                        x.ExpressionAttributeValues.First().Value.S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(key, It.Is<Dictionary<string, string>>(x =>
            x.First().Key == result.First().Key &&
            x.First().Value == result.First().Value &&
            x.Last().Key == result.Last().Key &&
            x.Last().Value == result.Last().Value
        ), duration), Times.Once);

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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(duration);

        // Act
        var result = await provider.GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.QueryAsync(
                    It.Is<QueryRequest>(x =>
                        x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                            .Split('=', StringSplitOptions.TrimEntries).Last() &&
                        x.ExpressionAttributeValues.First().Value.S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(key, It.Is<Dictionary<string, string>>(x =>
            x.First().Key == result.First().Key &&
            x.First().Value == result.First().Value &&
            x.Last().Key == result.Last().Key &&
            x.Last().Value == result.Last().Value
        ), duration), Times.Once);

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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await provider
            .WithMaxAge(duration)
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.QueryAsync(
                    It.Is<QueryRequest>(x =>
                        x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                            .Split('=', StringSplitOptions.TrimEntries).Last() &&
                        x.ExpressionAttributeValues.First().Value.S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(key, It.Is<Dictionary<string, string>>(x =>
            x.First().Key == result.First().Key &&
            x.First().Value == result.First().Value &&
            x.Last().Key == result.Last().Key &&
            x.Last().Value == result.Last().Value
        ), duration), Times.Once);

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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .UseTable(tableName);

        // Act
        var result = await provider
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.QueryAsync(
                    It.Is<QueryRequest>(x =>
                        x.TableName == tableName &&
                        x.KeyConditionExpression.Split('=', StringSplitOptions.TrimEntries).First() ==
                        primaryKeyAttribute &&
                        x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                            .Split('=', StringSplitOptions.TrimEntries).Last() &&
                        x.ExpressionAttributeValues.First().Value.S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);

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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonDynamoDB>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var provider = new DynamoDBProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .UseTable(tableName, primaryKeyAttribute, sortKeyAttribute, valueAttribute);

        // Act
        var result = await provider
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.QueryAsync(
                    It.Is<QueryRequest>(x =>
                        x.TableName == tableName &&
                        x.KeyConditionExpression.Split('=', StringSplitOptions.TrimEntries).First() ==
                        primaryKeyAttribute &&
                        x.ExpressionAttributeValues.First().Key == x.KeyConditionExpression
                            .Split('=', StringSplitOptions.TrimEntries).Last() &&
                        x.ExpressionAttributeValues.First().Value.S == key
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(result);
        Assert.Equal(value.First().Key, result.First().Key);
        Assert.Equal(value.First().Value, result.First().Value);
        Assert.Equal(value.Last().Key, result.Last().Key);
        Assert.Equal(value.Last().Value, result.Last().Value);
    }
}