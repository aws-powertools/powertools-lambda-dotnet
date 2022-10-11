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
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.AppConfig;

public class AppConfigProviderTest
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
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object);
        appConfigProvider.SetHandler(providerHandler.Object);
        appConfigProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);
        appConfigProvider.DefaultMaxAge(duration);
        appConfigProvider.AddTransformer(transformerName, transformer.Object);

        // Act
        var result = await appConfigProvider.GetAsync(key);

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
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object);
        appConfigProvider.SetHandler(providerHandler.Object);
        appConfigProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await appConfigProvider
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
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object);
        appConfigProvider.SetHandler(providerHandler.Object);
        appConfigProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await appConfigProvider
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
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformer = new Mock<ITransformer>();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, null)
        ).ReturnsAsync(value);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object);
        appConfigProvider.SetHandler(providerHandler.Object);
        appConfigProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await appConfigProvider
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
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformation = Transformation.Auto;
        var providerHandler = new Mock<IParameterProviderBaseHandler>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), transformation, null)
        ).ReturnsAsync(value);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object);
        appConfigProvider.SetHandler(providerHandler.Object);
        appConfigProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await appConfigProvider
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
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var transformerName = Guid.NewGuid().ToString();
        var providerHandler = new Mock<IParameterProviderBaseHandler>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        providerHandler.Setup(c =>
            c.GetAsync<string>(key, It.IsAny<ParameterProviderConfiguration?>(), null, transformerName)
        ).ReturnsAsync(value);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object);
        appConfigProvider.SetHandler(providerHandler.Object);
        appConfigProvider.UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await appConfigProvider
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
        var valueFromCache = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(new GetLatestConfigurationResponse());

        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        var result = await appConfigProvider.GetAsync(key);

        // Assert
        client.Verify(v =>
                v.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.StartConfigurationSessionAsync(It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response1);

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(valueFromCache);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.ForceFetch().GetAsync();

        // Assert
        cacheManager.Verify(v => v.Get(cacheKey), Times.Never);
        client.Verify(v =>
                v.StartConfigurationSessionAsync(
                    It.Is<StartConfigurationSessionRequest>(x =>
                        x.ApplicationIdentifier == applicationId &&
                        x.EnvironmentIdentifier == environmentId &&
                        x.ConfigurationProfileIdentifier == configProfileId
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        client.Verify(v =>
                v.GetLatestConfigurationAsync(
                    It.Is<GetLatestConfigurationRequest>(x =>
                        x.ConfigurationToken == configurationToken
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.StartConfigurationSessionAsync(It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response1);

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(null);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Verify(v => v.Get(cacheKey), Times.Once);
        cacheManager.Verify(
            v => v.Set(cacheKey,
                It.Is<IDictionary<string, string>>(d =>
                    d.First().Key == "Config1" &&
                    d.First().Value == value.Config1 &&
                    d.Last().Key == "Config2" &&
                    d.Last().Value == value.Config2
                ), duration),
            Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.StartConfigurationSessionAsync(It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response1);

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(null);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId)
            .WithMaxAge(duration);

        // Act
        var result = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Verify(v => v.Get(cacheKey), Times.Once);
        cacheManager.Verify(
            v => v.Set(cacheKey,
                It.Is<IDictionary<string, string>>(d =>
                    d.First().Key == "Config1" &&
                    d.First().Value == value.Config1 &&
                    d.Last().Key == "Config2" &&
                    d.Last().Value == value.Config2
                ), duration),
            Times.Once);
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
        var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var response2 = new GetLatestConfigurationResponse
        {
            Configuration = new MemoryStream(content),
            ContentType = contentType,
            ContentLength = content.Length
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.StartConfigurationSessionAsync(It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response1);

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(null);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultMaxAge(defaultMaxAge)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var result = await appConfigProvider.WithMaxAge(duration).GetAsync();

        // Assert
        cacheManager.Verify(v => v.Get(cacheKey), Times.Once);
        cacheManager.Verify(
            v => v.Set(cacheKey,
                It.Is<IDictionary<string, string>>(d =>
                    d.First().Key == "Config1" &&
                    d.First().Value == value.Config1 &&
                    d.Last().Key == "Config2" &&
                    d.Last().Value == value.Config2
                ), duration),
            Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.StartConfigurationSessionAsync(It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response1);

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(null);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.StartConfigurationSessionAsync(It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response1);

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(null);

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultEnvironment(environmentId)
            .DefaultConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetAsync_DefaultEnvironmentIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultApplication(applicationId)
            .DefaultConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetAsync_DefaultConfigProfileIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var environmentId = Guid.NewGuid().ToString();
        var applicationId = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .DefaultApplication(applicationId)
            .DefaultEnvironment(environmentId);

        // Act
        Task<IDictionary<string, string>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }
    
    [Fact]
    public async Task GetAsync_WhenApplicationIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var environmentId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetAsync_WhenEnvironmentIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var configProfileId = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetAsync_WhenConfigProfileIdDoesNotSet_ThrowsException()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var environmentId = Guid.NewGuid().ToString();

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId);

        // Act
        Task<IDictionary<string, string>> Act() => appConfigProvider.GetAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task GetMultipleAsync_ThrowsException()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object);

        // Act
        Task<IDictionary<string, string>> Act() => appConfigProvider.GetMultipleAsync(key);

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
        var lastConfig = new Dictionary<string, string>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(null);

        dateTimeWrapper.Setup(c =>
            c.UtcNow
        ).Returns(dateTimeNow);

        var appConfigProvider = new AppConfigProvider(
                dateTimeWrapper.Object,
                configurationToken,
                nextAllowedPollTime,
                lastConfig)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var currentConfig = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Verify(v => v.Get(cacheKey), Times.Once);
        client.Verify(v =>
                v.StartConfigurationSessionAsync(
                    It.IsAny<StartConfigurationSessionRequest>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
        client.Verify(v =>
                v.GetLatestConfigurationAsync(
                    It.IsAny<GetLatestConfigurationRequest>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
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

        var lastConfig = new Dictionary<string, string>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var value = new Dictionary<string, string>
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(null);

        dateTimeWrapper.Setup(c =>
            c.UtcNow
        ).Returns(dateTimeNow);

        var appConfigProvider = new AppConfigProvider(
                dateTimeWrapper.Object,
                configurationToken,
                nextAllowedPollTime,
                lastConfig)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var currentConfig = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Verify(v => v.Get(cacheKey), Times.Once);
        client.Verify(v =>
                v.StartConfigurationSessionAsync(
                    It.Is<StartConfigurationSessionRequest>(x =>
                        x.ApplicationIdentifier == applicationId &&
                        x.EnvironmentIdentifier == environmentId &&
                        x.ConfigurationProfileIdentifier == configProfileId
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Never);
        client.Verify(v =>
                v.GetLatestConfigurationAsync(
                    It.Is<GetLatestConfigurationRequest>(x =>
                        x.ConfigurationToken == configurationToken
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
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

        var lastConfig = new Dictionary<string, string>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var value = new Dictionary<string, string>
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.StartConfigurationSessionAsync(It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response1);

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(null);

        dateTimeWrapper.Setup(c =>
            c.UtcNow
        ).Returns(dateTimeNow);

        var appConfigProvider = new AppConfigProvider(
                dateTimeWrapper.Object,
                configurationToken,
                nextAllowedPollTime,
                lastConfig)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        var currentConfig = await appConfigProvider.GetAsync();

        // Assert
        cacheManager.Verify(v => v.Get(cacheKey), Times.Once);
        client.Verify(v =>
                v.StartConfigurationSessionAsync(
                    It.Is<StartConfigurationSessionRequest>(x =>
                        x.ApplicationIdentifier == applicationId &&
                        x.EnvironmentIdentifier == environmentId &&
                        x.ConfigurationProfileIdentifier == configProfileId
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        client.Verify(v =>
                v.GetLatestConfigurationAsync(
                    It.Is<GetLatestConfigurationRequest>(x =>
                        x.ConfigurationToken == configurationToken
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
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

        var lastConfig = new Dictionary<string, string>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var value = new Dictionary<string, string>
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        client.Setup(c =>
            c.StartConfigurationSessionAsync(It.IsAny<StartConfigurationSessionRequest>(),
                It.IsAny<CancellationToken>())
        ).ReturnsAsync(response1);

        client.Setup(c =>
            c.GetLatestConfigurationAsync(It.IsAny<GetLatestConfigurationRequest>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(response2);

        cacheManager.Setup(c =>
            c.Get(cacheKey)
        ).Returns(null);

        dateTimeWrapper.Setup(c =>
            c.UtcNow
        ).Returns(dateTimeNow);

        var appConfigProvider = new AppConfigProvider(
                dateTimeWrapper.Object,
                configurationToken,
                nextAllowedPollTime,
                lastConfig)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId)
            .ForceFetch();

        // Act
        var currentConfig = await appConfigProvider.GetAsync();

        // Assert
        client.Verify(v =>
                v.StartConfigurationSessionAsync(
                    It.Is<StartConfigurationSessionRequest>(x =>
                        x.ApplicationIdentifier == applicationId &&
                        x.EnvironmentIdentifier == environmentId &&
                        x.ConfigurationProfileIdentifier == configProfileId
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
        client.Verify(v =>
                v.GetLatestConfigurationAsync(
                    It.Is<GetLatestConfigurationRequest>(x =>
                        x.ConfigurationToken == configurationToken
                    ),
                    It.IsAny<CancellationToken>()),
            Times.Once);
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

        var cacheManager = new Mock<ICacheManager>();
        var client = new Mock<IAmazonAppConfigData>();
        var transformerManager = new Mock<ITransformerManager>();
        var dateTimeWrapper = new Mock<IDateTimeWrapper>();

        var appConfigProvider = new AppConfigProvider(dateTimeWrapper.Object)
            .UseClient(client.Object)
            .UseCacheManager(cacheManager.Object)
            .UseTransformerManager(transformerManager.Object)
            .WithApplication(applicationId)
            .WithEnvironment(environmentId)
            .WithConfigProfile(configProfileId);

        // Act
        Task<IDictionary<string, string>> Act() => appConfigProvider.GetMultipleAsync(key);

        await Assert.ThrowsAsync<NotSupportedException>(Act);
    }
}