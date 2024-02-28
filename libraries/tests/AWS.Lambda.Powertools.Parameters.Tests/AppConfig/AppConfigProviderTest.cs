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
using System.Text.Json;
using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;
using AWS.Lambda.Powertools.Parameters.AppConfig;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.AppConfig;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.AppConfig;

public class AppConfigProviderTest
{
    [Fact]
    public async Task GetAsync_SetupProvider_CallsHandler()
    {
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformerName = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var transformer = Substitute.For<ITransformer>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);
        var appConfig = new Dictionary<string, string?> { { key, value } };

        providerHandler
            .GetAsync<IDictionary<string, string?>>(cacheKey, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(appConfig);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper);
        appConfigProvider.SetHandler(providerHandler);
        appConfigProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);
        appConfigProvider.DefaultMaxAge(duration);
        appConfigProvider.AddTransformer(transformerName, transformer);
        appConfigProvider.DefaultApplication(applicationId);
        appConfigProvider.DefaultEnvironment(environmentId);
        appConfigProvider.DefaultConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.GetAsync(key);

        // Assert
        await providerHandler.Received(1).GetAsync<IDictionary<string, string?>>(cacheKey,
            Arg.Is<AppConfigProviderConfiguration?>(x =>
                x != null && x.ApplicationId == applicationId && x.EnvironmentId == environmentId &&
                x.ConfigProfileId == configProfileId), null, null);
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
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);
        var appConfig = new Dictionary<string, string?> { { key, value } };

        providerHandler
            .GetAsync<IDictionary<string, string?>>(cacheKey, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(appConfig);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper);
        appConfigProvider.SetHandler(providerHandler);
        appConfigProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);
        appConfigProvider.DefaultApplication(applicationId);
        appConfigProvider.DefaultEnvironment(environmentId);
        appConfigProvider.DefaultConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider
            .ForceFetch()
            .GetAsync(key);

        // Assert
        await providerHandler.Received(1).GetAsync<IDictionary<string, string?>>(cacheKey,
            Arg.Is<AppConfigProviderConfiguration?>(x =>
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
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var providerHandler = Substitute.For<IParameterProviderBaseHandler>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);
        var appConfig = new Dictionary<string, string?> { { key, value } };

        providerHandler
            .GetAsync<IDictionary<string, string?>>(cacheKey, Arg.Any<ParameterProviderConfiguration?>(), null, null)
            .Returns(appConfig);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper);
        appConfigProvider.SetHandler(providerHandler);
        appConfigProvider.UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);
        appConfigProvider.DefaultApplication(applicationId);
        appConfigProvider.DefaultEnvironment(environmentId);
        appConfigProvider.DefaultConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider
            .WithMaxAge(duration)
            .GetAsync(key);

        // Assert
        await providerHandler.Received(1).GetAsync<IDictionary<string, string?>>(cacheKey,
            Arg.Is<ParameterProviderConfiguration?>(x =>
                x != null && x.MaxAge == duration
            ), null,
            null);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenCachedObjectExists_ReturnsCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var valueFromCache = Guid.NewGuid().ToString();
        var appConfig = new Dictionary<string, string?> { { key, valueFromCache } };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetLatestConfigurationResponse());

        cacheManager.Get(cacheKey).Returns(appConfig);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        appConfigProvider.DefaultApplication(applicationId);
        appConfigProvider.DefaultEnvironment(environmentId);
        appConfigProvider.DefaultConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.GetAsync(key);

        // Assert
        await client.DidNotReceiveWithAnyArgs().GetLatestConfigurationAsync(null);
        Assert.NotNull(result);
        Assert.Equal(valueFromCache, result);
    }

    [Fact]
    public async Task GetAsync_WhenForceFetch_IgnoresCachedObject()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var value = new
        {
            Config1 = Guid.NewGuid().ToString(),
            Config2 = Guid.NewGuid().ToString()
        };

        var valueFromCache = new Dictionary<string, string>
        {
            { value.Config1, Guid.NewGuid().ToString() },
            { value.Config2, Guid.NewGuid().ToString() }
        };

        var response1 = new StartConfigurationSessionResponse
        {
            InitialConfigurationToken = configurationToken
        };

        var contentType = "application/json";
        var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        client.StartConfigurationSessionAsync(Arg.Any<StartConfigurationSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(response1);

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(response2);

        cacheManager.Get(cacheKey).Returns(valueFromCache);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.ForceFetch().GetAsync();

        // Assert
        cacheManager.DidNotReceive().Get(cacheKey);
        await client.Received(1).StartConfigurationSessionAsync(Arg.Is<StartConfigurationSessionRequest>(
            x => x.ApplicationIdentifier == applicationId &&
                 x.EnvironmentIdentifier == environmentId &&
                 x.ConfigurationProfileIdentifier == configProfileId), Arg.Any<CancellationToken>());
        await client.Received(1).GetLatestConfigurationAsync(Arg.Is<GetLatestConfigurationRequest>(
            x => x.ConfigurationToken == configurationToken), Arg.Any<CancellationToken>());
        Assert.NotNull(result);
        Assert.Equal("Config1", result.First().Key);
        Assert.Equal(value.Config1, result.First().Value);
        Assert.Equal("Config2", result.Last().Key);
        Assert.Equal(value.Config2, result.Last().Value);
    }

    [Fact]
    public async Task GetAsync_WhenMaxAgeNotSet_StoresCachedObjectWithDefaultMaxAge()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge;
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var value = new
        {
            Config1 = Guid.NewGuid().ToString(),
            Config2 = Guid.NewGuid().ToString()
        };

        var response1 = new StartConfigurationSessionResponse
        {
            InitialConfigurationToken = configurationToken
        };

        var contentType = "application/json";
        var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        client.StartConfigurationSessionAsync(Arg.Any<StartConfigurationSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(response1);

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(response2);

        cacheManager.Get(cacheKey).ReturnsNull();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Received(1).Get(cacheKey);
        cacheManager.Received(1).Set(cacheKey, Arg.Is<IDictionary<string, string>>(d =>
            d.First().Key == "Config1" &&
            d.First().Value == value.Config1 &&
            d.Last().Key == "Config2" &&
            d.Last().Value == value.Config2
        ), duration);
        Assert.NotNull(result);
        Assert.Equal("Config1", result.First().Key);
        Assert.Equal(value.Config1, result.First().Value);
        Assert.Equal("Config2", result.Last().Key);
        Assert.Equal(value.Config2, result.Last().Value);
    }

    [Fact]
    public async Task GetAsync_WhenMaxAgeClientSet_StoresCachedObjectWithDefaultMaxAge()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var value = new
        {
            Config1 = Guid.NewGuid().ToString(),
            Config2 = Guid.NewGuid().ToString()
        };

        var response1 = new StartConfigurationSessionResponse
        {
            InitialConfigurationToken = configurationToken
        };

        var contentType = "application/json";
        var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        client.StartConfigurationSessionAsync(Arg.Any<StartConfigurationSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(response1);

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(response2);

        cacheManager.Get(cacheKey).ReturnsNull();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId)
            .WithMaxAge(duration);

        // Act
        var result = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Received(1).Get(cacheKey);
        cacheManager.Received(1).Set(cacheKey, Arg.Is<IDictionary<string, string>>(d =>
            d.First().Key == "Config1" &&
            d.First().Value == value.Config1 &&
            d.Last().Key == "Config2" &&
            d.Last().Value == value.Config2
        ), duration);
        Assert.NotNull(result);
        Assert.Equal("Config1", result.First().Key);
        Assert.Equal(value.Config1, result.First().Value);
        Assert.Equal("Config2", result.Last().Key);
        Assert.Equal(value.Config2, result.Last().Value);
    }

    [Fact]
    public async Task GetAsync_WhenMaxAgeSet_StoresCachedObjectWithMaxAge()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = Guid.NewGuid().ToString();
        var defaultMaxAge = CacheManager.DefaultMaxAge;
        var duration = defaultMaxAge.Add(TimeSpan.FromHours(10));
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var value = new
        {
            Config1 = Guid.NewGuid().ToString(),
            Config2 = Guid.NewGuid().ToString()
        };

        var response1 = new StartConfigurationSessionResponse
        {
            InitialConfigurationToken = configurationToken
        };

        var contentType = "application/json";
        var jsonStr = JsonSerializer.Serialize(value);
        var content = Encoding.UTF8.GetBytes(jsonStr);
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        client.StartConfigurationSessionAsync(Arg.Any<StartConfigurationSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(response1);

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(response2);

        cacheManager.Get(cacheKey).ReturnsNull();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultMaxAge(defaultMaxAge)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.WithMaxAge(duration).GetAsync();

        // Assert
        cacheManager.Received(1).Get(cacheKey);
        cacheManager.Received(1).Set(cacheKey, Arg.Is<IDictionary<string, string>>(d =>
            d.First().Key == "Config1" &&
            d.First().Value == value.Config1 &&
            d.Last().Key == "Config2" &&
            d.Last().Value == value.Config2
        ), duration);
        Assert.NotNull(result);
        Assert.Equal("Config1", result.First().Key);
        Assert.Equal(value.Config1, result.First().Value);
        Assert.Equal("Config2", result.Last().Key);
        Assert.Equal(value.Config2, result.Last().Value);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsKeyValue()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var value = new
        {
            Config1 = Guid.NewGuid().ToString(),
            Config2 = Guid.NewGuid().ToString()
        };

        var response1 = new StartConfigurationSessionResponse
        {
            InitialConfigurationToken = configurationToken
        };

        var contentType = "application/json";
        var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        client.StartConfigurationSessionAsync(Arg.Any<StartConfigurationSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(response1);

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(response2);

        cacheManager.Get(cacheKey).ReturnsNull();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.GetAsync("Config1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Config1, result);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var value = new
        {
            Config1 = Guid.NewGuid().ToString(),
            Config2 = Guid.NewGuid().ToString()
        };

        var response1 = new StartConfigurationSessionResponse
        {
            InitialConfigurationToken = configurationToken
        };

        var contentType = "application/json";
        var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        client.StartConfigurationSessionAsync(Arg.Any<StartConfigurationSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(response1);

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(response2);

        cacheManager.Get(cacheKey).ReturnsNull();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.GetAsync("Config3");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_DefaultApplicationIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultEnvironment(environmentId)
            .DefaultConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string?>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetAsync_DefaultEnvironmentIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultApplication(applicationId)
            .DefaultConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string?>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetAsync_DefaultConfigProfileIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var environmentId = Guid.NewGuid().ToString();
        var applicationId = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .DefaultApplication(applicationId)
            .DefaultEnvironment(environmentId);

        // Act
        Task<IDictionary<string, string?>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetAsync_WhenApplicationIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string?>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetAsync_WhenEnvironmentIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string?>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetAsync_WhenConfigProfileIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId);

        // Act
        Task<IDictionary<string, string?>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetMultipleAsync_ThrowsException()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager);

        // Act
        Task<IDictionary<string, string?>> Act() => appConfigProvider.GetMultipleAsync(key);

        // Assert
        await Assert.ThrowsAsync<NotSupportedException>(Act);
    }

    [Fact]
    public async Task GetAsync_PriorToNextAllowedPollTime_ReturnsLastConfig()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var dateTimeNow = DateTime.UtcNow;
        var nextAllowedPollTime = dateTimeNow.AddSeconds(10);
        var lastConfig = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        cacheManager.Get(cacheKey).Returns(null);
        dateTimeWrapper.UtcNow.Returns(dateTimeNow);

        var appConfigResult = new AppConfigResult
        {
            PollConfigurationToken = configurationToken,
            NextAllowedPollTime = nextAllowedPollTime,
            LastConfig = JsonSerializer.Serialize(lastConfig)
        };

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper,
                AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId),
                appConfigResult)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var currentConfig = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Received(1).Get(cacheKey);
        await client.DidNotReceiveWithAnyArgs().StartConfigurationSessionAsync(null);
        await client.DidNotReceiveWithAnyArgs().GetLatestConfigurationAsync(null);
        Assert.NotNull(lastConfig);
        Assert.NotNull(currentConfig);
        Assert.Equal(lastConfig, currentConfig);
    }

    [Fact]
    public async Task GetAsync_AfterNextAllowedPollTime_RetrieveNewConfig()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var dateTimeNow = DateTime.UtcNow;
        var nextAllowedPollTime = dateTimeNow.AddSeconds(-1);
        var nextPollInterval = TimeSpan.FromHours(24);
        var nextPollConfigurationToken = Guid.NewGuid().ToString();

        var lastConfig = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var contentType = "application/json";
        var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length,
            NextPollConfigurationToken = nextPollConfigurationToken,
            NextPollIntervalInSeconds = Convert.ToInt32(nextPollInterval.TotalSeconds)
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(response2);

        cacheManager.Get(cacheKey).Returns(null);
        dateTimeWrapper.UtcNow.Returns(dateTimeNow);

        var appConfigResult = new AppConfigResult
        {
            PollConfigurationToken = configurationToken,
            NextAllowedPollTime = nextAllowedPollTime,
            LastConfig = JsonSerializer.Serialize(lastConfig)
        };

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper,
                AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId),
                appConfigResult)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var currentConfig = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Received(1).Get(cacheKey);
        await client.DidNotReceiveWithAnyArgs().StartConfigurationSessionAsync(null);
        await client.Received(1).GetLatestConfigurationAsync(
            Arg.Is<GetLatestConfigurationRequest>(x => x.ConfigurationToken == configurationToken),
            Arg.Any<CancellationToken>());
        Assert.NotNull(lastConfig);
        Assert.NotNull(currentConfig);
        Assert.NotEqual(lastConfig, currentConfig);
    }

    [Fact]
    public async Task GetAsync_WhenNoToken_StartsASessionAndRetrieveNewConfig()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = string.Empty;
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var dateTimeNow = DateTime.UtcNow;
        var nextAllowedPollTime = dateTimeNow.AddSeconds(-1);
        var nextPollInterval = TimeSpan.FromHours(24);
        var nextPollConfigurationToken = Guid.NewGuid().ToString();

        var lastConfig = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var response1 = new StartConfigurationSessionResponse
        {
            InitialConfigurationToken = configurationToken
        };

        var contentType = "application/json";
        var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length,
            NextPollConfigurationToken = nextPollConfigurationToken,
            NextPollIntervalInSeconds = Convert.ToInt32(nextPollInterval.TotalSeconds)
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        client.StartConfigurationSessionAsync(Arg.Any<StartConfigurationSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(response1);

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(response2);

        cacheManager.Get(cacheKey).Returns(null);
        dateTimeWrapper.UtcNow.Returns(dateTimeNow);

        var appConfigResult = new AppConfigResult
        {
            PollConfigurationToken = configurationToken,
            NextAllowedPollTime = nextAllowedPollTime,
            LastConfig = JsonSerializer.Serialize(lastConfig)
        };

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper,
                AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId),
                appConfigResult)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var currentConfig = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Received(1).Get(cacheKey);
        await client.Received(1).StartConfigurationSessionAsync(
            Arg.Is<StartConfigurationSessionRequest>(x =>
                x.ApplicationIdentifier == applicationId &&
                x.EnvironmentIdentifier == environmentId &&
                x.ConfigurationProfileIdentifier == configProfileId),
            Arg.Any<CancellationToken>());
        await client.Received(1).GetLatestConfigurationAsync(
            Arg.Is<GetLatestConfigurationRequest>(x => x.ConfigurationToken == configurationToken),
            Arg.Any<CancellationToken>());
        Assert.NotNull(lastConfig);
        Assert.NotNull(currentConfig);
        Assert.NotEqual(lastConfig, currentConfig);
    }

    [Fact]
    public async Task GetAsync_WhenForceFetch_RetrieveNewConfig()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();
        var configurationToken = Guid.NewGuid().ToString();
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId);

        var dateTimeNow = DateTime.UtcNow;
        var nextAllowedPollTime = dateTimeNow.AddSeconds(10);
        var nextPollInterval = TimeSpan.FromHours(24);
        var nextPollConfigurationToken = Guid.NewGuid().ToString();

        var lastConfig = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var response1 = new StartConfigurationSessionResponse
        {
            InitialConfigurationToken = configurationToken
        };

        var contentType = "application/json";
        var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length,
            NextPollConfigurationToken = nextPollConfigurationToken,
            NextPollIntervalInSeconds = Convert.ToInt32(nextPollInterval.TotalSeconds)
        };

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        client.StartConfigurationSessionAsync(Arg.Any<StartConfigurationSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(response1);

        client.GetLatestConfigurationAsync(Arg.Any<GetLatestConfigurationRequest>(), Arg.Any<CancellationToken>())
            .Returns(response2);

        cacheManager.Get(cacheKey).Returns(null);

        dateTimeWrapper.UtcNow.Returns(dateTimeNow);

        var appConfigResult = new AppConfigResult
        {
            PollConfigurationToken = configurationToken,
            NextAllowedPollTime = nextAllowedPollTime,
            LastConfig = JsonSerializer.Serialize(lastConfig)
        };

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper,
                AppConfigProviderCacheHelper.GetCacheKey(applicationId, environmentId, configProfileId),
                appConfigResult)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId)
            .ForceFetch();

        // Act
        var currentConfig = await appConfigProvider.GetAsync();

        // Assert
        await client.Received(1).StartConfigurationSessionAsync(
            Arg.Is<StartConfigurationSessionRequest>(x =>
                x.ApplicationIdentifier == applicationId &&
                x.EnvironmentIdentifier == environmentId &&
                x.ConfigurationProfileIdentifier == configProfileId),
            Arg.Any<CancellationToken>());
        await client.Received(1).GetLatestConfigurationAsync(
            Arg.Is<GetLatestConfigurationRequest>(x => x.ConfigurationToken == configurationToken),
            Arg.Any<CancellationToken>());
        Assert.NotNull(lastConfig);
        Assert.NotNull(currentConfig);
        Assert.NotEqual(lastConfig, currentConfig);
    }

    [Fact]
    public async Task GetMultipleAsync_WithArguments_ThrowsException()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();

        var cacheManager = Substitute.For<ICacheManager>();
        var client = Substitute.For<IAmazonAppConfigData>();
        var transformerManager = Substitute.For<ITransformerManager>();
        var dateTimeWrapper = Substitute.For<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper)
            .UseClient(client)
            .UseCacheManager(cacheManager)
            .UseTransformerManager(transformerManager)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string?>> Act() => appConfigProvider.GetMultipleAsync(key);

        await Assert.ThrowsAsync<NotSupportedException>(Act);
    }
}