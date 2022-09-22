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
using AWS.Lambda.Powertools.Parameters.Provider.Internal;
using AWS.Lambda.Powertools.Parameters.Transform;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.Provider;

public class ParameterProviderTest
{
    public interface IParameterProviderProxy
    {
        Task<string?> GetAsync(string key, ParameterProviderConfiguration? config);

        Task<IDictionary<string, string>> GetMultipleAsync(string path,
            ParameterProviderConfiguration? config);
    }
    
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

        var providerHandler = new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync);
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

        var providerHandler = new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync);
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

        var providerHandler = new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync);
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

        var providerHandler = new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync);
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

        var providerHandler = new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync);
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

        var providerHandler = new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync);
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
    public async Task GetAsync_WhenTransformationSet_ReturnsTransformedValue()
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

        var providerHandler = new ParameterProviderBaseHandler(providerProxy.Object.GetAsync, providerProxy.Object.GetMultipleAsync);
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
}