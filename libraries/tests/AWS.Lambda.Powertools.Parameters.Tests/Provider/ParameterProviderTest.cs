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

using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.Provider;

public class ParameterProviderTest
{
    public interface IParameterProviderProxy
    {
        Task<string?> GetAsync(string key, ParameterProviderConfiguration? config);

        Task<IDictionary<string, string?>> GetMultipleAsync(string path,
            ParameterProviderConfiguration? config);
    }

    #region GetAsync

    [Fact]
    public async Task GetAsync_WhenCachedObjectExists_ReturnsCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var valueFromCache = Guid.NewGuid().ToString();

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Never);
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
        var config = new ParameterProviderConfiguration
        {
            ForceFetch = true
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Never);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
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

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
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
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var config = new ParameterProviderConfiguration
        {
            MaxAge = duration
        };


        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenCacheModeIsGetResultOnly_StoresCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var cacheMode = ParameterProviderCacheMode.GetResultOnly;
        var config = new ParameterProviderConfiguration
        {
            MaxAge = duration
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                cacheMode);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }
    
    [Fact]
    public async Task GetAsync_WhenCacheModeIsGetMultipleResultOnly_DoesNotStoreCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var cacheMode = ParameterProviderCacheMode.GetMultipleResultOnly;
        var config = new ParameterProviderConfiguration
        {
            MaxAge = duration
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                cacheMode);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Never);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }
    
    [Fact]
    public async Task GetAsync_WhenCacheModeIsDisabled_DoesNotStoreCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var cacheMode = ParameterProviderCacheMode.Disabled;
        var config = new ParameterProviderConfiguration
        {
            MaxAge = duration
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                cacheMode);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Never);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenTransformerSet_ReturnsTransformedValue()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformedValue = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge;

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value)
        ).Returns(transformedValue);

        var config = new ParameterProviderConfiguration
        {
            Transformer = transformer.Object
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        transformer.Verify(v => v.Transform<string>(value), Times.Once);
        cacheManager.Verify(v => v.Set(key, transformedValue, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(transformedValue, result);
    }

    [Fact]
    public async Task GetAsync_WhenTransformerNameSet_ReturnsTransformedValue()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformedValue = Guid.NewGuid().ToString();
        var transformerName = Guid.NewGuid().ToString();
        var duration = CacheManager.DefaultMaxAge;

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value)
        ).Returns(transformedValue);

        var transformerManager = new Mock<ITransformerManager>();
        transformerManager.Setup(c =>
            c.GetTransformer(transformerName)
        ).Returns(transformer.Object);

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);
        providerHandler.SetTransformerManager(transformerManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, null, transformerName);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        transformer.Verify(v => v.Transform<string>(value), Times.Once);
        cacheManager.Verify(v => v.Set(key, transformedValue, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(transformedValue, result);
    }

    [Fact]
    public async Task GetAsync_WhenJsonTransformationSet_ReturnsTransformedValue()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformedValue = Guid.NewGuid().ToString();
        var transformation = Transformation.Json;
        var duration = CacheManager.DefaultMaxAge;

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value)
        ).Returns(transformedValue);

        var transformerManager = new Mock<ITransformerManager>();
        transformerManager.Setup(c =>
            c.TryGetTransformer(transformation, key)
        ).Returns(transformer.Object);

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);
        providerHandler.SetTransformerManager(transformerManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, transformation, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        transformer.Verify(v => v.Transform<string>(value), Times.Once);
        cacheManager.Verify(v => v.Set(key, transformedValue, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(transformedValue, result);
    }
    
    [Fact]
    public async Task GetAsync_WhenRaiseTransformationErrorNotSet_ReturnsNullOnError()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformationError = new Exception("Test Error");
        var transformation = Transformation.Json;

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value)
        ).Throws(transformationError);

        var transformerManager = new Mock<ITransformerManager>();
        transformerManager.Setup(c =>
            c.TryGetTransformer(transformation, key)
        ).Returns(transformer.Object);

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);
        providerHandler.SetTransformerManager(transformerManager.Object);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, transformation, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        transformer.Verify(v => v.Transform<string>(value), Times.Once);
        cacheManager.Verify(v => v.Set(key, It.IsAny<object?>(), It.IsAny<TimeSpan>()), Times.Never);
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetAsync_WhenRaiseTransformationErrorSet_ThrowsException()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformationError = new Exception("Test Error");
        var transformation = Transformation.Json;
        var raiseTransformationError = true;

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value)
        ).Throws(transformationError);

        var transformerManager = new Mock<ITransformerManager>();
        transformerManager.Setup(c =>
            c.TryGetTransformer(transformation, key)
        ).Returns(transformer.Object);

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);
        providerHandler.SetTransformerManager(transformerManager.Object);
        providerHandler.SetRaiseTransformationError(raiseTransformationError);

        // Act
        Task<string?> Act() => providerHandler.GetAsync<string>(key, null, transformation, null);

        // Assert
        await Assert.ThrowsAsync<TransformationException>(Act);
    }

    #endregion

    #region GetMultipleAsync

    [Fact]
    public async Task GetMultipleAsync_WhenCachedObjectExists_ReturnsCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var valueFromCache = new Dictionary<string, string?>
        {
            { value.First().Key, Guid.NewGuid().ToString() },
            { value.Last().Key, Guid.NewGuid().ToString() }
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, null, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Never);
        Assert.NotNull(result);
        Assert.Equal(valueFromCache, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenForceFetch_IgnoresCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var valueFromCache = new Dictionary<string, string?>
        {
            { value.First().Key, Guid.NewGuid().ToString() },
            { value.Last().Key, Guid.NewGuid().ToString() }
        };
        var config = new ParameterProviderConfiguration
        {
            ForceFetch = true
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(valueFromCache);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Never);
        providerProxy.Verify(v => v.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenMaxAgeNotSet_StoresCachedObjectWithDefaultMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge;

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, null, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenMaxAgeSet_StoresCachedObjectWithMaxAge()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var config = new ParameterProviderConfiguration
        {
            MaxAge = duration
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(key), Times.Once);
        providerProxy.Verify(v => v.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenCacheModeIsGetMultipleResultOnly_StoresCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var cacheMode = ParameterProviderCacheMode.GetMultipleResultOnly;
        var config = new ParameterProviderConfiguration
        {
            MaxAge = duration
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                cacheMode);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        providerProxy.Verify(v => v.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Once);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenCacheModeIsGetResultOnly_DoesNotStoreCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var cacheMode = ParameterProviderCacheMode.GetResultOnly;
        var config = new ParameterProviderConfiguration
        {
            MaxAge = duration
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                cacheMode);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        providerProxy.Verify(v => v.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Never);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }
    
    [Fact]
    public async Task GetMultipleAsync_WhenCacheModeDisabled_DoesNotStoreCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge.Add(TimeSpan.FromHours(10));
        var cacheMode = ParameterProviderCacheMode.Disabled;
        var config = new ParameterProviderConfiguration
        {
            MaxAge = duration
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(key)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                cacheMode);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        providerProxy.Verify(v => v.GetMultipleAsync(key, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        cacheManager.Verify(v => v.Set(key, value, duration), Times.Never);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }
    
    [Fact]
    public async Task GetMultipleAsync_WhenTransformerSet_ReturnsTransformedValue()
    {
        // Arrange
        var path = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var transformedValue = new Dictionary<string, string?>
        {
            { value.First().Key, Guid.NewGuid().ToString() },
            { value.Last().Key, Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge;

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value.First().Value ?? "")
        ).Returns(transformedValue.First().Value);
        transformer.Setup(c =>
            c.Transform<string>(value.Last().Value ?? "")
        ).Returns(transformedValue.Last().Value);

        var config = new ParameterProviderConfiguration
        {
            Transformer = transformer.Object
        };

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(path)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(path, config, null, null);

        // Assert
        cacheManager.Verify(v => v.Get(path), Times.Once);
        providerProxy.Verify(v => v.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        transformer.Verify(v => v.Transform<string>(value.First().Value ?? ""), Times.Once);
        transformer.Verify(v => v.Transform<string>(value.Last().Value ?? ""), Times.Once);
        cacheManager.Verify(v => v.Set(path, It.Is<Dictionary<string, string>>(o =>
            o.First().Key == transformedValue.First().Key &&
            o.First().Value == transformedValue.First().Value &&
            o.Last().Key == transformedValue.Last().Key &&
            o.Last().Value == transformedValue.Last().Value
        ), duration), Times.Once);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenTransformerNameSet_ReturnsTransformedValue()
    {
        // Arrange
        var path = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var transformedValue = new Dictionary<string, string?>
        {
            { value.First().Key, Guid.NewGuid().ToString() },
            { value.Last().Key, Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge;
        var transformerName = Guid.NewGuid().ToString();

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value.First().Value ?? "")
        ).Returns(transformedValue.First().Value);
        transformer.Setup(c =>
            c.Transform<string>(value.Last().Value ?? "")
        ).Returns(transformedValue.Last().Value);

        var transformerManager = new Mock<ITransformerManager>();
        transformerManager.Setup(c =>
            c.GetTransformer(transformerName)
        ).Returns(transformer.Object);

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(path)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);
        providerHandler.SetTransformerManager(transformerManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(path, null, null, transformerName);

        // Assert
        cacheManager.Verify(v => v.Get(path), Times.Once);
        providerProxy.Verify(v => v.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        transformer.Verify(v => v.Transform<string>(value.First().Value ?? ""), Times.Once);
        transformer.Verify(v => v.Transform<string>(value.Last().Value ?? ""), Times.Once);
        cacheManager.Verify(v => v.Set(path, It.Is<Dictionary<string, string>>(o =>
            o.First().Key == transformedValue.First().Key &&
            o.First().Value == transformedValue.First().Value &&
            o.Last().Key == transformedValue.Last().Key &&
            o.Last().Value == transformedValue.Last().Value
        ), duration), Times.Once);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenTransformationSet_ReturnsTransformedValue()
    {
        // Arrange
        var path = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };
        var transformedValue = new Dictionary<string, string?>
        {
            { value.First().Key, Guid.NewGuid().ToString() },
            { value.Last().Key, Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge;

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value.First().Value ?? "")
        ).Returns(transformedValue.First().Value);
        transformer.Setup(c =>
            c.Transform<string>(value.Last().Value ?? "")
        ).Returns(transformedValue.Last().Value);

        var transformation = Transformation.Json;
        var transformerManager = new Mock<ITransformerManager>();
        transformerManager.Setup(c =>
            c.GetTransformer(transformation)
        ).Returns(transformer.Object);

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(path)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);
        providerHandler.SetTransformerManager(transformerManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(path, null, transformation, null);

        // Assert
        cacheManager.Verify(v => v.Get(path), Times.Once);
        providerProxy.Verify(v => v.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        transformer.Verify(v => v.Transform<string>(value.First().Value ?? ""), Times.Once);
        transformer.Verify(v => v.Transform<string>(value.Last().Value ?? ""), Times.Once);
        cacheManager.Verify(v => v.Set(path, It.Is<Dictionary<string, string>>(o =>
            o.First().Key == transformedValue.First().Key &&
            o.First().Value == transformedValue.First().Value &&
            o.Last().Key == transformedValue.Last().Key &&
            o.Last().Value == transformedValue.Last().Value
        ), duration), Times.Once);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenTransformationAuto_ReturnsTransformedValue()
    {
        // Arrange
        var path = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { $"{Guid.NewGuid()}.json", Guid.NewGuid().ToString() },
            { $"{Guid.NewGuid()}.binary", Guid.NewGuid().ToString() }
        };
        var transformedValue = new Dictionary<string, string?>
        {
            { value.First().Key, Guid.NewGuid().ToString() },
            { value.Last().Key, Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge;

        var jsonTransformer = new Mock<ITransformer>();
        jsonTransformer.Setup(c =>
            c.Transform<string>(value.First().Value ?? "")
        ).Returns(transformedValue.First().Value ?? "");

        var base64Transformer = new Mock<ITransformer>();
        base64Transformer.Setup(c =>
            c.Transform<string>(value.Last().Value ?? "")
        ).Returns(transformedValue.Last().Value);

        var transformation = Transformation.Auto;
        var transformerManager = new Mock<ITransformerManager>();
        transformerManager.Setup(c =>
            c.TryGetTransformer(transformation, value.First().Key)
        ).Returns(jsonTransformer.Object);
        transformerManager.Setup(c =>
            c.TryGetTransformer(transformation, value.Last().Key)
        ).Returns(base64Transformer.Object);

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(path)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);
        providerHandler.SetTransformerManager(transformerManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(path, null, transformation, null);

        // Assert
        cacheManager.Verify(v => v.Get(path), Times.Once);
        providerProxy.Verify(v => v.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        jsonTransformer.Verify(v => v.Transform<string>(value.First().Value ?? ""), Times.Once);
        base64Transformer.Verify(v => v.Transform<string>(value.Last().Value ?? ""), Times.Once);
        cacheManager.Verify(v => v.Set(path, It.Is<Dictionary<string, string>>(o =>
            o.First().Key == transformedValue.First().Key &&
            o.First().Value == transformedValue.First().Value &&
            o.Last().Key == transformedValue.Last().Key &&
            o.Last().Value == transformedValue.Last().Value
        ), duration), Times.Once);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMultipleAsync_WhenRaiseTransformationErrorNotSet_ReturnsNullOnError()
    {
        // Arrange
        var path = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var transformedValue = Guid.NewGuid().ToString();
        var transformationError = new Exception("Test Error");
        var duration = CacheManager.DefaultMaxAge;

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value.First().Value ?? "")
        ).Throws(transformationError);

        transformer.Setup(c =>
            c.Transform<string>(value.Last().Value ?? "")
        ).Returns(transformedValue);

        var transformation = Transformation.Json;
        var transformerManager = new Mock<ITransformerManager>();
        transformerManager.Setup(c =>
            c.GetTransformer(transformation)
        ).Returns(transformer.Object);

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(path)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);
        providerHandler.SetTransformerManager(transformerManager.Object);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(path, null, transformation, null);

        // Assert
        cacheManager.Verify(v => v.Get(path), Times.Once);
        providerProxy.Verify(v => v.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>()), Times.Once);
        transformer.Verify(v => v.Transform<string>(value.First().Value ?? ""), Times.Once);
        transformer.Verify(v => v.Transform<string>(value.Last().Value ?? ""), Times.Once);
        cacheManager.Verify(v => v.Set(path, It.Is<Dictionary<string, string?>>(o =>
            o.First().Key == value.First().Key &&
            o.First().Value == null &&
            o.Last().Key == value.Last().Key &&
            o.Last().Value == transformedValue
        ), duration), Times.Once);
        
        Assert.NotNull(result);
    }
    
    [Fact]
    public async Task GetMultipleAsync_WhenRaiseTransformationErrorSet_ThrowsException()
    {
        // Arrange
        var path = Guid.NewGuid().ToString();
        var value = new Dictionary<string, string?>
        {
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
        };

        var transformedValue = Guid.NewGuid().ToString();
        var transformationError = new Exception("Test Error");
        var raiseTransformationError = true;

        var transformer = new Mock<ITransformer>();
        transformer.Setup(c =>
            c.Transform<string>(value.First().Value ?? "")
        ).Throws(transformationError);

        transformer.Setup(c =>
            c.Transform<string>(value.Last().Value ?? "")
        ).Returns(transformedValue);

        var transformation = Transformation.Json;
        var transformerManager = new Mock<ITransformerManager>();
        transformerManager.Setup(c =>
            c.GetTransformer(transformation)
        ).Returns(transformer.Object);

        var providerProxy = new Mock<IParameterProviderProxy>();
        providerProxy.Setup(c =>
            c.GetMultipleAsync(path, It.IsAny<ParameterProviderConfiguration?>())
        ).ReturnsAsync(value);

        var cacheManager = new Mock<ICacheManager>();
        cacheManager.Setup(c =>
            c.Get(path)
        ).Returns(null);

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync,
                ParameterProviderCacheMode.All);
        providerHandler.SetCacheManager(cacheManager.Object);
        providerHandler.SetTransformerManager(transformerManager.Object);
        providerHandler.SetRaiseTransformationError(raiseTransformationError);
        
        // Act
        Task<IDictionary<string, string?>> Act() => providerHandler.GetMultipleAsync<string>(path, null, transformation, null);

        // Assert
        await Assert.ThrowsAsync<TransformationException>(Act);
    }

    #endregion
}