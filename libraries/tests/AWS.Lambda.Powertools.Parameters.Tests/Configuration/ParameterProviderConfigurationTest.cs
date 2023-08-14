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

using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.Configuration;

public class ParameterProviderConfigurationTest
{
    [Fact]
    public async Task GetAsync_WithMaxAge_CallsProvidersWithTheConfiguration()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var duration = TimeSpan.FromSeconds(5);

        var provider = Substitute.For<IParameterProviderBaseHandler>();
        provider.GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.MaxAge == duration),
            Arg.Is<Transformation?>(x => x == null),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        ).Returns(value);

        var providerConfigurationBuilder = new ParameterProviderConfigurationBuilder(provider);
        providerConfigurationBuilder.WithMaxAge(duration);

        // Act
        var result = await providerConfigurationBuilder.GetAsync(key);

        // Assert
        await provider.Received(1).GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.MaxAge == duration),
            Arg.Is<Transformation?>(x => x == null),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WhenForceFetch_CallsProvidersWithTheConfiguration()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var provider = Substitute.For<IParameterProviderBaseHandler>();
        provider.GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.ForceFetch),
            Arg.Is<Transformation?>(x => x == null),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        ).Returns(value);

        var providerConfigurationBuilder = new ParameterProviderConfigurationBuilder(provider);
        providerConfigurationBuilder.ForceFetch();

        // Act
        var result = await providerConfigurationBuilder.GetAsync(key);

        // Assert
        await provider.Received(1).GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.ForceFetch),
            Arg.Is<Transformation?>(x => x == null),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformation_CallsProvidersWithTheConfiguration()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();

        var transformer = Substitute.For<ITransformer>();
        var provider = Substitute.For<IParameterProviderBaseHandler>();
        provider.GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == transformer),
            Arg.Is<Transformation?>(x => x == null),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        ).Returns(value);

        var providerConfigurationBuilder = new ParameterProviderConfigurationBuilder(provider);
        providerConfigurationBuilder.WithTransformation(transformer);

        // Act
        var result = await providerConfigurationBuilder.GetAsync(key);

        // Assert
        await provider.Received(1).GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == transformer),
            Arg.Is<Transformation?>(x => x == null),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformationJson_CallsProvidersWithTheConfiguration()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformation = Transformation.Json;

        var provider = Substitute.For<IParameterProviderBaseHandler>();
        provider.GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == null),
            Arg.Is<Transformation?>(x => x == transformation),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        ).Returns(value);

        var providerConfigurationBuilder = new ParameterProviderConfigurationBuilder(provider);
        providerConfigurationBuilder.WithTransformation(transformation);

        // Act
        var result = await providerConfigurationBuilder.GetAsync(key);

        // Assert
        await provider.Received(1).GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == null),
            Arg.Is<Transformation?>(x => x == transformation),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformationBase64_CallsProvidersWithTheConfiguration()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformation = Transformation.Base64;

        var provider = Substitute.For<IParameterProviderBaseHandler>();
        provider.GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == null),
            Arg.Is<Transformation?>(x => x == transformation),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        ).Returns(value);

        var providerConfigurationBuilder = new ParameterProviderConfigurationBuilder(provider);
        providerConfigurationBuilder.WithTransformation(transformation);

        // Act
        var result = await providerConfigurationBuilder.GetAsync(key);

        // Assert
        await provider.Received(1).GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == null),
            Arg.Is<Transformation?>(x => x == transformation),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformationAuto_CallsProvidersWithTheConfiguration()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformation = Transformation.Auto;

        var provider = Substitute.For<IParameterProviderBaseHandler>();
        provider.GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == null),
            Arg.Is<Transformation?>(x => x == transformation),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        ).Returns(value);

        var providerConfigurationBuilder = new ParameterProviderConfigurationBuilder(provider);
        providerConfigurationBuilder.WithTransformation(transformation);

        // Act
        var result = await providerConfigurationBuilder.GetAsync(key);

        // Assert
        await provider.Received(1).GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == null),
            Arg.Is<Transformation?>(x => x == transformation),
            Arg.Is<string?>(x => string.IsNullOrEmpty(x))
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithTransformationName_CallsProvidersWithTheConfiguration()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var transformationName = Guid.NewGuid().ToString();

        var provider = Substitute.For<IParameterProviderBaseHandler>();
        provider.GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == null),
            Arg.Is<Transformation?>(x => x == null),
            Arg.Is<string?>(x => x == transformationName)
        ).Returns(value);

        var providerConfigurationBuilder = new ParameterProviderConfigurationBuilder(provider);
        providerConfigurationBuilder.WithTransformation(transformationName);

        // Act
        var result = await providerConfigurationBuilder.GetAsync(key);

        // Assert
        await provider.Received(1).GetAsync<string>(
            key,
            Arg.Is<ParameterProviderConfiguration?>(x => x != null && x.Transformer == null),
            Arg.Is<Transformation?>(x => x == null),
            Arg.Is<string?>(x => x == transformationName)
        );
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }
}