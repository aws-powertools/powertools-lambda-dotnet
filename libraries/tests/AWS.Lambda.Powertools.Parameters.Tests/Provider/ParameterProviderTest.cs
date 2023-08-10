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

using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(valueFromCache);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync, 
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All, 
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.DidNotReceive().GetAsync(key, Arg.Any<ParameterProviderConfiguration>());
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(valueFromCache);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.DidNotReceive().Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Is<ParameterProviderConfiguration?>(x => x!.ForceFetch));
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Any<ParameterProviderConfiguration?>());
        cacheManager.Received(1).Set(key, value, duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);
        
        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync, 
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All, 
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Any<ParameterProviderConfiguration?>());
        cacheManager.Received(1).Set(key, value, duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync, 
            providerProxy.GetMultipleAsync,
            cacheMode, 
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Any<ParameterProviderConfiguration?>());
        cacheManager.Received(1).Set(key, value, duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            cacheMode,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Any<ParameterProviderConfiguration?>());
        cacheManager.Received(0).Set(key, value, duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            cacheMode,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Any<ParameterProviderConfiguration?>());
        cacheManager.Received(0).Set(key, value, duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value).Returns(transformedValue);

        var config = new ParameterProviderConfiguration
        {
            Transformer = transformer
        };

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Any<ParameterProviderConfiguration?>());
        transformer.Received(1).Transform<string>(value);
        cacheManager.Received(1).Set(key, transformedValue, duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value).Returns(transformedValue);

        var transformerManager = Substitute.For<ITransformerManager>();
        transformerManager.GetTransformer(transformerName).Returns(transformer);

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);
        providerHandler.SetTransformerManager(transformerManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, null, transformerName);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Any<ParameterProviderConfiguration?>());
        transformer.Received(1).Transform<string>(value);
        cacheManager.Received(1).Set(key, transformedValue, duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value).Returns(transformedValue);

        var transformerManager = Substitute.For<ITransformerManager>();
        transformerManager.TryGetTransformer(transformation, key).Returns(transformer);

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!
            .Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);
        providerHandler.SetTransformerManager(transformerManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, transformation, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Any<ParameterProviderConfiguration?>());
        transformer.Received(1).Transform<string>(value);
        cacheManager.Received(1).Set(key, transformedValue, duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value).Throws(transformationError);

        var transformerManager = Substitute.For<ITransformerManager>();
        transformerManager.TryGetTransformer(transformation, key).Returns(transformer);

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!.Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);
        providerHandler.SetTransformerManager(transformerManager);

        // Act
        var result = await providerHandler.GetAsync<string>(key, null, transformation, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetAsync(key, Arg.Any<ParameterProviderConfiguration?>());
        transformer.Received(1).Transform<string>(value);
        cacheManager.DidNotReceive().Set(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<TimeSpan>());
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value).Throws(transformationError);

        var transformerManager = Substitute.For<ITransformerManager>();
        transformerManager.TryGetTransformer(transformation, key).Returns(transformer);

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetAsync(key, Arg.Any<ParameterProviderConfiguration?>())!.Returns(Task.FromResult(value));

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);
        providerHandler.SetTransformerManager(transformerManager);
        providerHandler.SetRaiseTransformationError(raiseTransformationError);

        // Act
        Task<string?> Act() => providerHandler.GetAsync<string>(key, null, transformation, null);

        // Assert
        await Assert.ThrowsAsync<TransformationException>(Act);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(valueFromCache);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.GetAsync, providerProxy.GetMultipleAsync,
                ParameterProviderCacheMode.All, powertoolsConfigurations);
        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, null, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(0).GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration>());
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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
        
        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(valueFromCache);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.GetAsync, providerProxy.GetMultipleAsync,
                ParameterProviderCacheMode.All, powertoolsConfigurations);
        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        // Assert
        cacheManager.Received(0).Get(key);
        await providerProxy.Received(1).GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration>());
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();
        
        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.GetAsync, providerProxy.GetMultipleAsync,
                ParameterProviderCacheMode.All, powertoolsConfigurations);
        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, null, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration>());
        cacheManager.Received(1).Set(key, Arg.Is<Dictionary<string, string?>>(x=> 
            x.First().Key == value.First().Key && 
            x.First().Value == value.First().Value &&
            x.Last().Key == value.Last().Key &&
            x.Last().Value == value.Last().Value &&
            x.Count == value.Count), duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.GetAsync, providerProxy.GetMultipleAsync,
                ParameterProviderCacheMode.All, powertoolsConfigurations);
        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        cacheManager.Received(1).Get(key);
        await providerProxy.Received(1).GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration>());
        cacheManager.Received(1).Set(key, Arg.Is<Dictionary<string, string?>>(x=> 
            x.First().Key == value.First().Key && 
            x.First().Value == value.First().Value &&
            x.Last().Key == value.Last().Key &&
            x.Last().Value == value.Last().Value &&
            x.Count == value.Count), duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(key, config).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.GetAsync, providerProxy.GetMultipleAsync,
                cacheMode, powertoolsConfigurations);
        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        await providerProxy.Received(1).GetMultipleAsync(key, config);
        cacheManager.Received(1).Set(key, Arg.Is<Dictionary<string, string?>>(x=> 
                x.First().Key == value.First().Key && 
                x.First().Value == value.First().Value &&
                x.Last().Key == value.Last().Key &&
                x.Last().Value == value.Last().Value &&
                x.Count == value.Count), duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.GetAsync, providerProxy.GetMultipleAsync,
                cacheMode, powertoolsConfigurations);
        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        await providerProxy.Received(1).GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration>());
        cacheManager.DidNotReceiveWithAnyArgs().Set(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<TimeSpan>());
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(key).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.GetAsync, providerProxy.GetMultipleAsync,
                cacheMode, powertoolsConfigurations);
        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(key, config, null, null);

        // Assert
        await providerProxy.Received(1).GetMultipleAsync(key, Arg.Any<ParameterProviderConfiguration>());
        cacheManager.DidNotReceiveWithAnyArgs().Set(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<TimeSpan>());
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value.First().Value ?? "").Returns(transformedValue.First().Value);
        transformer.Transform<string>(value.Last().Value ?? "").Returns(transformedValue.Last().Value);

        var config = new ParameterProviderConfiguration
        {
            Transformer = transformer
        };

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(path).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.GetAsync, providerProxy.GetMultipleAsync,
                ParameterProviderCacheMode.All, powertoolsConfigurations);
        providerHandler.SetCacheManager(cacheManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(path, config, null, null);

        // Assert
        cacheManager.Received(1).Get(path);
        await providerProxy.Received(1).GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration>());
        transformer.Received(1).Transform<string>(value.First().Value ?? "");
        transformer.Received(1).Transform<string>(value.Last().Value ?? "");
        cacheManager.Received(1).Set(
            path,
            Arg.Is<Dictionary<string, string>>(o =>
                o.First().Key == transformedValue.First().Key &&
                o.First().Value == transformedValue.First().Value &&
                o.Last().Key == transformedValue.Last().Key &&
                o.Last().Value == transformedValue.Last().Value
            ),
            duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value.First().Value ?? "").Returns(transformedValue.First().Value);
        transformer.Transform<string>(value.Last().Value ?? "").Returns(transformedValue.Last().Value);

        var transformerManager = Substitute.For<ITransformerManager>();
        transformerManager.GetTransformer(transformerName).Returns(transformer);

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(path).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler =
            new ParameterProviderBaseHandler(providerProxy.GetAsync, providerProxy.GetMultipleAsync,
                ParameterProviderCacheMode.All, powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);
        providerHandler.SetTransformerManager(transformerManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(path, null, null, transformerName);

        // Assert
        cacheManager.Received(1).Get(path);
        await providerProxy.Received(1).GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration>());
        transformer.Received(1).Transform<string>(value.First().Value ?? "");
        transformer.Received(1).Transform<string>(value.Last().Value ?? "");
        cacheManager.Received(1).Set(
            path,
            Arg.Is<Dictionary<string, string>>(o =>
                o.First().Key == transformedValue.First().Key &&
                o.First().Value == transformedValue.First().Value &&
                o.Last().Key == transformedValue.Last().Key &&
                o.Last().Value == transformedValue.Last().Value
            ),
            duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value.First().Value ?? "").Returns(transformedValue.First().Value);
        transformer.Transform<string>(value.Last().Value ?? "").Returns(transformedValue.Last().Value);

        var transformation = Transformation.Json;
        var transformerManager = Substitute.For<ITransformerManager>();
        transformerManager.GetTransformer(transformation).Returns(transformer);

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(path).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);
        providerHandler.SetTransformerManager(transformerManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(path, null, transformation, null);

        // Assert
        cacheManager.Received(1).Get(path);
        await providerProxy.Received(1).GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration>());
        transformer.Received(1).Transform<string>(value.First().Value ?? "");
        transformer.Received(1).Transform<string>(value.Last().Value ?? "");
        cacheManager.Received(1).Set(
            path,
            Arg.Is<Dictionary<string, string>>(o =>
                o.First().Key == transformedValue.First().Key &&
                o.First().Value == transformedValue.First().Value &&
                o.Last().Key == transformedValue.Last().Key &&
                o.Last().Value == transformedValue.Last().Value
            ),
            duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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
        var transformedValue = new Dictionary<string, object?>
        {
            {
                value.First().Key, new
                {
                    FirstName = Guid.NewGuid().ToString(),
                    LastName = Guid.NewGuid().ToString()
                }
            },
            { value.Last().Key, Guid.NewGuid().ToString() }
        };
        var duration = CacheManager.DefaultMaxAge;

        var jsonTransformer = Substitute.For<ITransformer>();
        jsonTransformer.Transform<object>(value.First().Value ?? "").Returns(transformedValue.First().Value);

        var base64Transformer = Substitute.For<ITransformer>();
        base64Transformer.Transform<object>(value.Last().Value ?? "").Returns(transformedValue.Last().Value);

        var transformation = Transformation.Auto;
        var transformerManager = Substitute.For<ITransformerManager>();
        transformerManager.TryGetTransformer(transformation, value.First().Key).Returns(jsonTransformer);
        transformerManager.TryGetTransformer(transformation, value.Last().Key).Returns(base64Transformer);

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(path).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);
        providerHandler.SetTransformerManager(transformerManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<object>(path, null, transformation, null);

        // Assert
        cacheManager.Received(1).Get(path);
        await providerProxy.Received(1).GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration>());
        jsonTransformer.Received(1).Transform<object>(value.First().Value ?? "");
        base64Transformer.Received(1).Transform<object>(value.Last().Value ?? "");
        cacheManager.Received(1).Set(
            path,
            Arg.Is<Dictionary<string, object?>>(o =>
                o.First().Key == transformedValue.First().Key &&
                o.First().Value == transformedValue.First().Value &&
                o.Last().Key == transformedValue.Last().Key &&
                o.Last().Value == transformedValue.Last().Value
            ),
            duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value.First().Value ?? "").Returns(_ => throw transformationError);
        transformer.Transform<string>(value.Last().Value ?? "").Returns(transformedValue);

        var transformation = Transformation.Json;
        var transformerManager = Substitute.For<ITransformerManager>();
        transformerManager.GetTransformer(transformation).Returns(transformer);

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(path).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);
        providerHandler.SetTransformerManager(transformerManager);

        // Act
        var result = await providerHandler.GetMultipleAsync<string>(path, null, transformation, null);

        // Assert
        cacheManager.Received(1).Get(path);
        await providerProxy.Received(1).GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration>());
        transformer.Received(1).Transform<string>(value.First().Value ?? "");
        transformer.Received(1).Transform<string>(value.Last().Value ?? "");
        cacheManager.Received(1).Set(
            path,
            Arg.Is<Dictionary<string, string?>>(o =>
                o.First().Key == value.First().Key &&
                o.First().Value == null &&
                o.Last().Key == value.Last().Key &&
                o.Last().Value == transformedValue
            ),
            duration);
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
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

        var transformer = Substitute.For<ITransformer>();
        transformer.Transform<string>(value.First().Value ?? "").Throws(transformationError);
        transformer.Transform<string>(value.Last().Value ?? "").Returns(transformedValue);

        var transformation = Transformation.Json;
        var transformerManager = Substitute.For<ITransformerManager>();
        transformerManager.GetTransformer(transformation).Returns(transformer);

        var providerProxy = Substitute.For<IParameterProviderProxy>();
        providerProxy.GetMultipleAsync(path, Arg.Any<ParameterProviderConfiguration?>()).Returns(value);

        var cacheManager = Substitute.For<ICacheManager>();
        cacheManager.Get(path).Returns(null);

        var powertoolsConfigurations = Substitute.For<IPowertoolsConfigurations>();

        var providerHandler = new ParameterProviderBaseHandler(
            providerProxy.GetAsync,
            providerProxy.GetMultipleAsync,
            ParameterProviderCacheMode.All,
            powertoolsConfigurations);

        providerHandler.SetCacheManager(cacheManager);
        providerHandler.SetTransformerManager(transformerManager);
        providerHandler.SetRaiseTransformationError(raiseTransformationError);

        // Act
        async Task<IDictionary<string, string?>> Act() =>
            await providerHandler.GetMultipleAsync<string>(path, null, transformation, null);

        // Assert
        powertoolsConfigurations.Received(1).SetExecutionEnvironment(providerHandler);
        await Assert.ThrowsAsync<TransformationException>(Act);
    }

    #endregion
}