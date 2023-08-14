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
using NSubstitute;
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

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler);
        secretsProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);
        secretsProvider.DefaultMaxAge(duration);
        secretsProvider.AddTransformer(transformerName, transformer);

        // Act
        var result = await secretsProvider.GetAsync(key);

        // Assert
        await providerHandler.Received(1).GetAsync<string>(key, null, null, null);
        providerHandler.Received(1).SetCacheManager(cacheManager);
        providerHandler.Received(1).SetTransformerManager(transformerManager);
        providerHandler.Received(1).SetDefaultMaxAge(duration);
        providerHandler.Received(1).AddCustomTransformer(transformerName, transformer);
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
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler);
        secretsProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await secretsProvider
            .ForceFetch()
            .GetAsync(key);

        // Assert
        await providerHandler.Received(1).GetAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.ForceFetch),
            null,
            null);
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
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler);
        secretsProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await secretsProvider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        await providerHandler.Received(1).GetAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.MaxAge == duration),
            null,
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
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler);
        secretsProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await secretsProvider
            .WithTransformation(transformer)
            .GetAsync(key);

        // Assert
        await providerHandler.Received(1).GetAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == transformer),
            null,
            null);
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
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && !x.ForceFetch),
            transformation,
            null
        )!.Returns(Task.FromResult(value));

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler);
        secretsProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await secretsProvider
            .WithTransformation(transformation)
            .GetAsync(key);

        // Assert
        await providerHandler.Received(1).GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && !x.ForceFetch),
            transformation,
            null
        );
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
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && !x.ForceFetch),
            null,
            transformerName
        )!.Returns(Task.FromResult(value));

        var secretsProvider = new SecretsProvider();
        secretsProvider.SetHandler(providerHandler);
        secretsProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await secretsProvider
            .WithTransformation(transformerName)
            .GetAsync(key);

        // Assert
        await providerHandler.Received(1).GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && !x.ForceFetch),
            null,
            transformerName
        );
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

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(valueFromCache);

        var secretsProvider = new SecretsProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await secretsProvider.GetAsync(key);

        // Assert
        await client.DidNotReceive()
            .GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>());
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

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(valueFromCache);

        var secretsProvider = new SecretsProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await secretsProvider.ForceFetch().GetAsync(key);

        // Assert
        cacheManager.DidNotReceive().Get(key);
        await client.Received(1).GetSecretValueAsync(
            Arg.Is<GetSecretValueRequest>(x => x.SecretId == key && x.VersionStage == CurrentVersionStage),
            Arg.Any<CancellationToken>());
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

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await secretsProvider.GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetSecretValueAsync(
            Arg.Is<GetSecretValueRequest>(x => x.SecretId == key && x.VersionStage == CurrentVersionStage),
            Arg.Any<CancellationToken>());
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
        var response = new GetSecretValueResponse
        {
            SecretString = value
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(duration);

        // Act
        var result = await secretsProvider.GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetSecretValueAsync(
            Arg.Is<GetSecretValueRequest>(x => x.SecretId == key && x.VersionStage == CurrentVersionStage),
            Arg.Any<CancellationToken>());
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
        var response = new GetSecretValueResponse
        {
            SecretString = value
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await secretsProvider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetSecretValueAsync(
            Arg.Is<GetSecretValueRequest>(x => x.SecretId == key && x.VersionStage == CurrentVersionStage),
            Arg.Any<CancellationToken>());
        cacheManager.Received(1).Set(key, value, duration);
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

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager.Get(key).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await secretsProvider.GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetSecretValueAsync(
            Arg.Is<GetSecretValueRequest>(x => x.SecretId == key && x.VersionStage == CurrentVersionStage),
            Arg.Any<CancellationToken>());
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_ThrowsException()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSecretsManager>();
        var transformerManager = Substitute.For<ITransformerManager>();

        cacheManager.Get(key).Returns(null);

        var secretsProvider = new SecretsProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        async Task<IDictionary<string, string?>> Act() => await secretsProvider.GetMultipleAsync(key);

        // Assert
        await Assert.ThrowsAsync<NotSupportedException>(Act);
    }
}