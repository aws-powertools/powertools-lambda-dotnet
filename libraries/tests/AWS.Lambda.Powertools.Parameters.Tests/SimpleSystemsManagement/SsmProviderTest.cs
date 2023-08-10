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
using NSubstitute;
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

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)!
            .Returns(Task.FromResult(value));

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);
        ssmProvider.DefaultMaxAge(duration);
        ssmProvider.AddTransformer(transformerName, transformer);

        // Act
        var result = await ssmProvider.GetAsync(key);

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
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)!
            .Returns(Task.FromResult(value));

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .ForceFetch()
            .GetAsync(key);

        // Assert
        await providerHandler.Received(1)
            .GetAsync<string>(key,
                Arg.Is<ParameterProviderConfiguration?>(x =>
                    x != null && x.ForceFetch
                ), null,
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
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)!
            .Returns(Task.FromResult(value));

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        await providerHandler
            .Received(1)
            .GetAsync<string>(key, Arg.Is<ParameterProviderConfiguration?>(
                x => x != null && x.MaxAge == duration
            ), null, null);
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
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)!
            .Returns(Task.FromResult(value));

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithTransformation(transformer)
            .GetAsync(key);

        // Assert
        await providerHandler
            .Received(1)
            .GetAsync<string>(key, Arg.Is<ParameterProviderConfiguration?>(
                x => x != null && x.Transformer == transformer
            ), null, null);
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
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), transformation, null)!
            .Returns(Task.FromResult(value));

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithTransformation(transformation)
            .GetAsync(key);

        // Assert
        await providerHandler
            .Received(1)
            .GetAsync<string>(key,
                Arg.Is<ParameterProviderConfiguration?>(
                    x => x != null && !x.ForceFetch
                ),
                Arg.Is<Transformation?>(x => x == transformation),
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
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler
            .GetAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, transformerName)!
            .Returns(Task.FromResult(value));

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithTransformation(transformerName)
            .GetAsync(key);

        // Assert
        await providerHandler
            .Received(1)
            .GetAsync<string>(key,
                Arg.Is<ParameterProviderConfiguration?>(
                    x => x != null && !x.ForceFetch
                ),
                null,
                Arg.Is<string?>(x => x == transformerName));
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

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client
            .GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager
            .Get(key)
            .Returns(valueFromCache);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider.GetAsync(key);

        // Assert
        await client
            .DidNotReceive()
            .GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>());
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

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client
            .GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        cacheManager
            .Get(key)
            .Returns(valueFromCache);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider.ForceFetch().GetAsync(key);

        // Assert
        cacheManager
            .DidNotReceive()
            .Get(key);
        await client
            .Received(1)
            .GetParameterAsync(
                Arg.Is<GetParameterRequest>(x => x.Name == key && !x.WithDecryption),
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
        var response = new GetParameterResponse
        {
            Parameter = new Parameter
            {
                Value = value
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider.GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1)
            .GetParameterAsync(Arg.Is<GetParameterRequest>(x =>
                x.Name == key && !x.WithDecryption), Arg.Any<CancellationToken>());
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
        var response = new GetParameterResponse
        {
            Parameter = new Parameter
            {
                Value = value
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(duration);

        // Act
        var result = await ssmProvider.GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1)
            .GetParameterAsync(Arg.Is<GetParameterRequest>(x =>
                x.Name == key && !x.WithDecryption), Arg.Any<CancellationToken>());
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
        var response = new GetParameterResponse
        {
            Parameter = new Parameter
            {
                Value = value
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await ssmProvider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1)
            .GetParameterAsync(Arg.Is<GetParameterRequest>(x =>
                x.Name == key && !x.WithDecryption), Arg.Any<CancellationToken>());
        cacheManager.Received(1).Set(key, value, duration);
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

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithDecryption()
            .GetAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1)
            .GetParameterAsync(Arg.Is<GetParameterRequest>(x =>
                x.Name == key && x.WithDecryption), Arg.Any<CancellationToken>());
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
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);
        ssmProvider.DefaultMaxAge(duration);
        ssmProvider.AddTransformer(transformerName, transformer);

        // Act
        var result = await ssmProvider.GetMultipleAsync(key);

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
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .ForceFetch()
            .GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.ForceFetch),
            null,
            null);
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
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithMaxAge(duration)
            .GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.MaxAge == duration),
            null,
            null);
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
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithTransformation(transformer)
            .GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == transformer),
            null,
            null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WithTransformation_CallsHandlerWithConfiguredParameters()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), transformation, null)
            .Returns(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithTransformation(transformation)
            .GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && !x.ForceFetch),
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
        var value = new Dictionary<string, string?>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();

        providerHandler.GetMultipleAsync<string>(key, Arg.Any<ParameterProviderConfiguration?>(), null, transformerName)
            .Returns(value);

        var ssmProvider = new SsmProvider();
        ssmProvider.SetHandler(providerHandler);
        ssmProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithTransformation(transformerName)
            .GetMultipleAsync(key);

        // Assert
        await providerHandler.Received(1).GetMultipleAsync<string>(key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && !x.ForceFetch),
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
        var valueFromCache = new Dictionary<string, string?>()
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var response = new GetParametersByPathResponse
        {
            Parameters = new List<Parameter>
            {
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(valueFromCache);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider.GetMultipleAsync(key);

        // Assert
        await client.DidNotReceiveWithAnyArgs().GetParametersByPathAsync(null, CancellationToken.None);
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
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };
        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(valueFromCache);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider.ForceFetch().GetMultipleAsync(key);

        // Assert
        cacheManager.DidNotReceiveWithAnyArgs().Get(Arg.Any<string>());
        await client.ReceivedWithAnyArgs(1).GetParametersByPathAsync(null, CancellationToken.None);
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
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider.GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(x =>
                x.Path == key && !x.WithDecryption
            ),
            Arg.Any<CancellationToken>()
        );

        foreach (var item in result)
        {
            cacheManager.Received(1).Set(item.Key, item.Value, duration);
        }

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
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(duration);

        // Act
        var result = await ssmProvider.GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(x =>
                x.Path == key && !x.WithDecryption
            ),
            Arg.Any<CancellationToken>()
        );

        foreach (var item in result)
        {
            cacheManager.Received(1).Set(item.Key, item.Value, duration);
        }

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
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(defaultMaxAge);

        // Act
        var result = await ssmProvider
            .WithMaxAge(duration)
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(x =>
                x.Path == key && !x.WithDecryption
            ),
            Arg.Any<CancellationToken>()
        );

        foreach (var item in result)
        {
            cacheManager.Received(1).Set(item.Key, item.Value, duration);
        }

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
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParametersByPathAsync(
            Arg.Any<GetParametersByPathRequest>(), Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .WithDecryption()
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(x =>
                x.Path == key && x.WithDecryption
            ),
            Arg.Any<CancellationToken>()
        );

        foreach (var item in result)
        {
            Assert.Contains(response.Parameters, p =>
                p.Name == item.Key && p.Value == item.Value
            );
        }
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
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                },
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParametersByPathAsync(
            Arg.Any<GetParametersByPathRequest>(), Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(response));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .Recursive()
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(x =>
                x.Path == key && x.Recursive
            ),
            Arg.Any<CancellationToken>()
        );

        foreach (var item in result)
        {
            Assert.Contains(response.Parameters, p =>
                p.Name == item.Key && p.Value == item.Value
            );
        }
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
                new Parameter
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
                new Parameter
                {
                    Name = Guid.NewGuid().ToString(),
                    Value = Guid.NewGuid().ToString()
                }
            }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonSimpleSystemsManagement>();
        var transformerManager = Substitute.For<ITransformerManager>();

        client.GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(x => string.IsNullOrEmpty(x.NextToken)),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(response1));

        client.GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(x => x.NextToken == nextToken),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(response2));

        cacheManager.Get(key).Returns(null);

        var ssmProvider = new SsmProvider()
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        var result = await ssmProvider
            .Recursive()
            .WithDecryption()
            .GetMultipleAsync(key);

        // Assert
        cacheManager.Received(1).Get(key);
        await client.Received(1).GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(x =>
                x.Path == key && x.Recursive && x.WithDecryption && string.IsNullOrEmpty(x.NextToken)
            ),
            Arg.Any<CancellationToken>()
        );

        await client.Received(1).GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(x =>
                x.Path == key && x.Recursive && x.WithDecryption && x.NextToken == nextToken
            ),
            Arg.Any<CancellationToken>()
        );

        foreach (var item in result)
        {
            Assert.Contains(response1.Parameters.Concat(response2.Parameters), p =>
                p.Name == item.Key && p.Value == item.Value
            );
        }
    }
}