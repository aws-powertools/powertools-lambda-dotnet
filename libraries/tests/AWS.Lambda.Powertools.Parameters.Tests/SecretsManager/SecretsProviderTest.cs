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

using System.Text;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.SecretsManager;
using AWS.Lambda.Powertools.Parameters.Transform;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.SecretsManager;

public class SecretsProviderTest
{
    private const string CurrentVersionStage = "AWSCURRENT";

    [Fact]
    public async Task GetAsync_SetupProvider_CallsHandler()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformerName = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler.Object);
        secretsProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);
        secretsProvider.DefaultMaxAge(duration);
        secretsProvider.AddTransformer(transformerName, transformer.Object);

        // Act
        var result = await secretsProvider.GetAsync(key);

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
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler.Object);
        secretsProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await secretsProvider
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
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler.Object);
        secretsProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await secretsProvider
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
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler.Object);
        secretsProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await secretsProvider
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
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), transformation, null)
        ).ReturnsAsync(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler.Object);
        secretsProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await secretsProvider
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
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, transformerName)
        ).ReturnsAsync(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler.Object);
        secretsProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await secretsProvider
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
        var response = new GetSecretValueResponse
        {
            SecretString = value
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var secretsProvider = new SecretsProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await secretsProvider.GetAsync(key);

        // Assert
        client.Verify(v =>
                v.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(),
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
        var response = new GetSecretValueResponse
        {
            SecretString = value
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var secretsProvider = new SecretsProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await secretsProvider.ForceFetch().GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Never);
        client.Verify(v =>
                v.GetSecretValueAsync(
                    It.Is<GetSecretValueRequest>(x =>
                        x.SecretId == key &&
                        x.VersionStage == CurrentVersionStage
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
        var response = new GetSecretValueResponse
        {
            SecretString = value
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await secretsProvider.GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetSecretValueAsync(
                    It.Is<GetSecretValueRequest>(x =>
                        x.SecretId == key &&
                        x.VersionStage == CurrentVersionStage
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
        var response = new GetSecretValueResponse
        {
            SecretString = value
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(duration);

        // Act
        var result = await secretsProvider.GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetSecretValueAsync(
                    It.Is<GetSecretValueRequest>(x =>
                        x.SecretId == key &&
                        x.VersionStage == CurrentVersionStage
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
        var response = new GetSecretValueResponse
        {
            SecretString = value
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await secretsProvider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetSecretValueAsync(
                    It.Is<GetSecretValueRequest>(x =>
                        x.SecretId == key &&
                        x.VersionStage == CurrentVersionStage
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenReturnsBinary_ReturnsAsString()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var plainTextBytes = Encoding.UTF8.GetBytes(value);
        var convertedValue = Convert.ToBase64String(plainTextBytes);
        var convertedByteArray = Encoding.UTF8.GetBytes(convertedValue);

        var response = new GetSecretValueResponse
        {
            SecretBinary = new MemoryStream(convertedByteArray)
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();

        client.Setup(c =>
            c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response);

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await secretsProvider.GetAsync(key);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        client.Verify(v =>
                v.GetSecretValueAsync(
                    It.Is<GetSecretValueRequest>(x =>
                        x.SecretId == key &&
                        x.VersionStage == CurrentVersionStage
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_ThrowsException()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonSecretsManager>();
        var transformerManager = new Mock<ITransformerManager>();

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        Task<IDictionary<string, string?>> Act() => secretsProvider.GetMultipleAsync(key);

        await Assert.ThrowsAsync<NotSupportedException>(Act);

    }
}