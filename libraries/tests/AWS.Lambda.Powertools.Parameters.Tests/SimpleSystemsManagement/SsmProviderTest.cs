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

using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.SimpleSystemsManagement;

public class SsmProviderTest
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);
        ssmProvider.DefaultMaxAge(duration);
        ssmProvider.AddTransformer(transformerName, transformer.Object);

        // Act
        var result = await ssmProvider.GetAsync(key);

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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), transformation, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, transformerName)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var response = new GetParameterResponse
        {
            Parameter = new Parameter
            {
                Value = value
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider.GetAsync(key);

        // Assert
        client.Verify(v =>
                v.GetParameterAsync(It.IsAny<GetParameterRequest>(),
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
        var response = new GetParameterResponse
        {
            Parameter = new Parameter
            {
                Value = value
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider.ForceFetch().GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Never);
        client.Verify(v =>
                v.GetParameterAsync(
                    It.Is<GetParameterRequest>(x =>
                        x.Name == key && !x.WithDecryption
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
        var response = new GetParameterResponse
        {
            Parameter = new Parameter
            {
                Value = value
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider.GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParameterAsync(
                    It.Is<GetParameterRequest>(x =>
                        x.Name == key && !x.WithDecryption
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
        var response = new GetParameterResponse
        {
            Parameter = new Parameter
            {
                Value = value
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(duration);

        // Act
        var result = await ssmProvider.GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParameterAsync(
                    It.Is<GetParameterRequest>(x =>
                        x.Name == key && !x.WithDecryption
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
        var response = new GetParameterResponse
        {
            Parameter = new Parameter
            {
                Value = value
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await ssmProvider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParameterAsync(
                    It.Is<GetParameterRequest>(x =>
                        x.Name == key && !x.WithDecryption
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithDecryption_CallsClientWithDecryption()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var response = new GetParameterResponse
        {
            Parameter = new Parameter
            {
                Value = value
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
            .WithDecryption()
            .GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParameterAsync(
                    It.Is<GetParameterRequest>(x =>
                        x.Name == key && x.WithDecryption
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);
        ssmProvider.DefaultMaxAge(duration);
        ssmProvider.AddTransformer(transformerName, transformer.Object);

        // Act
        var result = await ssmProvider.GetMultipleAsync(key);

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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), transformation, null)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>(), null, transformerName)
        ).ReturnsAsync(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler.Object);
        ssmProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
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
        var response = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParametersByPathAsync(It.IsAny<GetParametersByPathRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider.GetMultipleAsync(key);

        // Assert
        client.Verify(v =>
                v.GetParametersByPathAsync(It.IsAny<GetParametersByPathRequest>(),
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
        var response = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };
        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParametersByPathAsync(It.IsAny<GetParametersByPathRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider.ForceFetch().GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Never);
        client.Verify(v =>
                v.GetParametersByPathAsync(
                    It.Is<GetParametersByPathRequest>(x =>
                        x.Path == key && !x.WithDecryption
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(result);
        Assert.Equal(response.Parameters.First().Name, result.First().Key);
        Assert.Equal(response.Parameters.First().Value, result.First().Value);
        Assert.Equal(response.Parameters.Last().Name, result.Last().Key);
        Assert.Equal(response.Parameters.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenMaxAgeNotSet_StoresCachedObjectWithDefaultMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge;
        var response = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParametersByPathAsync(It.IsAny<GetParametersByPathRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider.GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParametersByPathAsync(
                    It.Is<GetParametersByPathRequest>(x =>
                        x.Path == key && !x.WithDecryption
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(result.First().Key, result.First().Value, duration), Times.Once);
        cacheManager.Verify(v => v.Set(result.Last().Key, result.Last().Value, duration), Times.Once);
        cacheManager.Verify(v => v.Set(key, It.Is<Dictionary<string, string>>(x =>
            x.First().Key == result.First().Key &&
            x.First().Value == result.First().Value &&
            x.Last().Key == result.Last().Key &&
            x.Last().Value == result.Last().Value
        ), duration), Times.Once);

        Assert.NotNull(result);
        Assert.Equal(response.Parameters.First().Name, result.First().Key);
        Assert.Equal(response.Parameters.First().Value, result.First().Value);
        Assert.Equal(response.Parameters.Last().Name, result.Last().Key);
        Assert.Equal(response.Parameters.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenMaxAgeClientSet_StoresCachedObjectWithDefaultMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var response = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParametersByPathAsync(It.IsAny<GetParametersByPathRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(duration);

        // Act
        var result = await ssmProvider.GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParametersByPathAsync(
                    It.Is<GetParametersByPathRequest>(x =>
                        x.Path == key && !x.WithDecryption
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(result.First().Key, result.First().Value, duration), Times.Once);
        cacheManager.Verify(v => v.Set(result.Last().Key, result.Last().Value, duration), Times.Once);
        cacheManager.Verify(v => v.Set(key, It.Is<Dictionary<string, string>>(x =>
            x.First().Key == result.First().Key &&
            x.First().Value == result.First().Value &&
            x.Last().Key == result.Last().Key &&
            x.Last().Value == result.Last().Value
        ), duration), Times.Once);

        Assert.NotNull(result);
        Assert.Equal(response.Parameters.First().Name, result.First().Key);
        Assert.Equal(response.Parameters.First().Value, result.First().Value);
        Assert.Equal(response.Parameters.Last().Name, result.Last().Key);
        Assert.Equal(response.Parameters.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenMaxAgeSet_StoresCachedObjectWithMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var defaultMaxAge = CacheManager.DefaultMaxAge;
        var duration = defaultMaxAge.Add(TimeSpan.FromHours(10));
        var response = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParametersByPathAsync(It.IsAny<GetParametersByPathRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await ssmProvider
            .WithMaxAge(duration)
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParametersByPathAsync(
                    It.Is<GetParametersByPathRequest>(x =>
                        x.Path == key && !x.WithDecryption
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(result.First().Key, result.First().Value, duration), Times.Once);
        cacheManager.Verify(v => v.Set(result.Last().Key, result.Last().Value, duration), Times.Once);
        cacheManager.Verify(v => v.Set(key, It.Is<Dictionary<string, string>>(x =>
            x.First().Key == result.First().Key &&
            x.First().Value == result.First().Value &&
            x.Last().Key == result.Last().Key &&
            x.Last().Value == result.Last().Value
        ), duration), Times.Once);

        Assert.NotNull(result);
        Assert.Equal(response.Parameters.First().Name, result.First().Key);
        Assert.Equal(response.Parameters.First().Value, result.First().Value);
        Assert.Equal(response.Parameters.Last().Name, result.Last().Key);
        Assert.Equal(response.Parameters.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WithDecryption_CallsClientWithDecryption()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var response = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParametersByPathAsync(It.IsAny<GetParametersByPathRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
            .WithDecryption()
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParametersByPathAsync(
                    It.Is<GetParametersByPathRequest>(x =>
                        x.Path == key && x.WithDecryption
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(result);
        Assert.Equal(response.Parameters.First().Name, result.First().Key);
        Assert.Equal(response.Parameters.First().Value, result.First().Value);
        Assert.Equal(response.Parameters.Last().Name, result.Last().Key);
        Assert.Equal(response.Parameters.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenRecursive_CallsClientRecursive()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var response = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParametersByPathAsync(It.IsAny<GetParametersByPathRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
            .Recursive()
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParametersByPathAsync(
                    It.Is<GetParametersByPathRequest>(x =>
                        x.Path == key && x.Recursive
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(result);
        Assert.Equal(response.Parameters.First().Name, result.First().Key);
        Assert.Equal(response.Parameters.First().Value, result.First().Value);
        Assert.Equal(response.Parameters.Last().Name, result.Last().Key);
        Assert.Equal(response.Parameters.Last().Value, result.Last().Value);
    }

    [Fact]
    public async Task GetMultipleAsync_WhileNextToken_RetrieveAll()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var nextToken = Guid.NewGuid().ToString();
        var response1 = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            },
            NextToken = nextToken,
        };
        var response2 = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetParametersByPathAsync(It.Is<GetParametersByPathRequest>(x => string.IsNullOrEmpty(x.NextToken)),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response1);

        client.Setup(c =>
            c.GetParametersByPathAsync(It.Is<GetParametersByPathRequest>(x => x.NextToken == nextToken),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await ssmProvider
            .Recursive()
            .WithDecryption()
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetParametersByPathAsync(
                    It.Is<GetParametersByPathRequest>(x =>
                        x.Path == key && x.Recursive && x.WithDecryption && string.IsNullOrEmpty(x.NextToken)
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        client.Verify(v =>
                v.GetParametersByPathAsync(
                    It.Is<GetParametersByPathRequest>(x =>
                        x.Path == key && x.Recursive && x.WithDecryption && x.NextToken == nextToken
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(result);

        Assert.Equal(response1.Parameters.First().Name, result.First().Key);
        Assert.Equal(response1.Parameters.First().Value, result.First().Value);
        Assert.Equal(response2.Parameters.First().Name, result.Last().Key);
        Assert.Equal(response2.Parameters.First().Value, result.Last().Value);
    }
}