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

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Idempotency.Tests.Handlers;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Internal;

[Collection("Sequential")]
public class IdempotentAspectTests : IDisposable
{
    [Theory]
    [InlineData(typeof(IdempotencyEnabledFunction))]
    [InlineData(typeof(IdempotencyEnabledSyncFunction))]
    public async Task Handle_WhenFirstCall_ShouldPutInStore(Type type)
    {
        //Arrange
        var store = Substitute.For<BasePersistenceStore>();
        Idempotency.Configure(builder =>
            builder
#if NET8_0_OR_GREATER
                .WithJsonSerializationContext(TestJsonSerializerContext.Default)
#endif
                .WithPersistenceStore(store)
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
        );

        var context = new TestLambdaContext
        {
            RemainingTime = TimeSpan.FromSeconds(30)
        };

        var function = Activator.CreateInstance(type) as IIdempotencyEnabledFunction;
        var product = new Product(42, "fake product", 12);

        //Act
        var basket = await function!.HandleTest(product, context);

        //Assert
        basket.Products.Count.Should().Be(1);
        function.HandlerExecuted.Should().BeTrue();

        await store.Received().SaveInProgress(
            Arg.Is<JsonDocument>(t =>
                t.ToString() == JsonSerializer.SerializeToDocument(product, new JsonSerializerOptions()).ToString()),
            Arg.Any<DateTimeOffset>(), Arg.Is<double>(d => d == 30000)
        );

        await store.Received().SaveSuccess(
            Arg.Any<JsonDocument>(),
            Arg.Is<Basket>(y => y.Equals(basket)),
            Arg.Any<DateTimeOffset>()
        );
    }

    [Theory]
    [InlineData(typeof(IdempotencyEnabledFunction))]
    [InlineData(typeof(IdempotencyEnabledSyncFunction))]
    public async Task Handle_WhenSecondCall_AndNotExpired_ShouldGetFromStore(Type type)
    {
        //Arrange
        var store = Substitute.For<BasePersistenceStore>();
        store.SaveInProgress(Arg.Any<JsonDocument>(), Arg.Any<DateTimeOffset>(), Arg.Any<double>())
            .Returns(_ => throw new IdempotencyItemAlreadyExistsException());

        // GIVEN
        Idempotency.Configure(builder =>
            builder
#if NET8_0_OR_GREATER
                .WithJsonSerializationContext(TestJsonSerializerContext.Default)
#endif
                .WithPersistenceStore(store)
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
        );

        var product = new Product(42, "fake product", 12);
        var basket = new Basket(product);
        var record = new DataRecord(
            "42",
            DataRecord.DataRecordStatus.COMPLETED,
            DateTimeOffset.UtcNow.AddSeconds(356).ToUnixTimeSeconds(),
            JsonSerializer.SerializeToNode(basket)!.ToString(),
            null);
        store.GetRecord(Arg.Any<JsonDocument>(), Arg.Any<DateTimeOffset>()).Returns(record);

        var function = Activator.CreateInstance(type) as IIdempotencyEnabledFunction;

        // Act
        var resultBasket = await function!.HandleTest(product, new TestLambdaContext());

        // Assert
        resultBasket.Should().Be(basket);
        function.HandlerExecuted.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(IdempotencyEnabledFunction))]
    [InlineData(typeof(IdempotencyEnabledSyncFunction))]
    public async Task Handle_WhenSecondCall_AndStatusInProgress_ShouldThrowIdempotencyAlreadyInProgressException(
        Type type)
    {
        // Arrange
        var store = Substitute.For<BasePersistenceStore>();

        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store)
#if NET8_0_OR_GREATER
                .WithJsonSerializationContext(TestJsonSerializerContext.Default)
#endif
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
        );

        store.SaveInProgress(Arg.Any<JsonDocument>(), Arg.Any<DateTimeOffset>(), Arg.Any<double>())
            .Returns(_ => throw new IdempotencyItemAlreadyExistsException());

        var product = new Product(42, "fake product", 12);
        var basket = new Basket(product);
        var record = new DataRecord(
            "42",
            DataRecord.DataRecordStatus.INPROGRESS,
            DateTimeOffset.UtcNow.AddSeconds(356).ToUnixTimeSeconds(),
            JsonSerializer.SerializeToNode(basket)!.ToString(),
            null);
        store.GetRecord(Arg.Any<JsonDocument>(), Arg.Any<DateTimeOffset>())
            .Returns(record);

        // Act
        var function = Activator.CreateInstance(type) as IIdempotencyEnabledFunction;
        Func<Task> act = async () => await function!.HandleTest(product, new TestLambdaContext());

        // Assert
        await act.Should().ThrowAsync<IdempotencyAlreadyInProgressException>();
    }

    [Theory]
    [InlineData(typeof(IdempotencyEnabledFunction))]
    [InlineData(typeof(IdempotencyEnabledSyncFunction))]
    public async Task
        Handle_WhenSecondCall_InProgress_LambdaTimeout_Expired_ShouldThrowIdempotencyInconsistentStateException(
            Type type)
    {
        // Arrange
        var store = Substitute.For<BasePersistenceStore>();

        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store)
#if NET8_0_OR_GREATER
                .WithJsonSerializationContext(TestJsonSerializerContext.Default)
#endif
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
        );

        store.SaveInProgress(Arg.Any<JsonDocument>(), Arg.Any<DateTimeOffset>(), Arg.Any<double>())
            .Returns(_ => throw new IdempotencyItemAlreadyExistsException());

        var timestampInThePast = DateTimeOffset.Now.AddSeconds(-30).ToUnixTimeMilliseconds();

        var product = new Product(42, "fake product", 12);
        var basket = new Basket(product);
        var record = new DataRecord(
            "42",
            DataRecord.DataRecordStatus.INPROGRESS,
            DateTimeOffset.UtcNow.AddSeconds(356).ToUnixTimeSeconds(),
            JsonSerializer.SerializeToNode(basket)!.ToString(),
            null,
            timestampInThePast);

        store.GetRecord(Arg.Any<JsonDocument>(), Arg.Any<DateTimeOffset>())
            .Returns(record);

        // Act
        var function = Activator.CreateInstance(type) as IIdempotencyEnabledFunction;
        Func<Task> act = async () => await function!.HandleTest(product, new TestLambdaContext());

        // Assert
        await act.Should().ThrowAsync<IdempotencyInconsistentStateException>();
    }

    [Theory]
    [InlineData(typeof(IdempotencyWithErrorFunction))]
    [InlineData(typeof(IdempotencyWithErrorSyncFunction))]
    public async Task Handle_WhenThrowException_ShouldDeleteRecord_AndThrowFunctionException(Type type)
    {
        // Arrange
        var store = Substitute.For<BasePersistenceStore>();

        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store)
#if NET8_0_OR_GREATER
                .WithJsonSerializationContext(TestJsonSerializerContext.Default)
#endif
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
        );

        var function = Activator.CreateInstance(type) as IIdempotencyWithErrorFunction;
        var product = new Product(42, "fake product", 12);

        // Act
        Func<Task> act = async () => await function!.HandleTest(product, new TestLambdaContext());

        // Assert
        await act.Should().ThrowAsync<IndexOutOfRangeException>();
        await store.Received().DeleteRecord(Arg.Any<JsonDocument>(), Arg.Any<IndexOutOfRangeException>());
    }

    [Theory]
    [InlineData(typeof(IdempotencyEnabledFunction))]
    [InlineData(typeof(IdempotencyEnabledSyncFunction))]
    public async Task Handle_WhenIdempotencyDisabled_ShouldJustRunTheFunction(Type type)
    {
        // Arrange
        var store = Substitute.For<BasePersistenceStore>();

        Environment.SetEnvironmentVariable(Constants.IdempotencyDisabledEnv, "true");

        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store)
#if NET8_0_OR_GREATER
                .WithJsonSerializationContext(TestJsonSerializerContext.Default)
#endif
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
        );

        var function = Activator.CreateInstance(type) as IIdempotencyEnabledFunction;
        var product = new Product(42, "fake product", 12);

        // Act
        var basket = await function!.HandleTest(product, new TestLambdaContext());

        // Assert
        store.ReceivedCalls().Count().Should().Be(0);
        basket.Products.Count.Should().Be(1);
        function.HandlerExecuted.Should().BeTrue();
    }

    [Fact]
    public void Idempotency_Set_Execution_Environment_Context()
    {
        // Arrange
        var assemblyName = "AWS.Lambda.Powertools.Idempotency";
        var assemblyVersion = "1.0.0";

        var env = Substitute.For<IPowertoolsEnvironment>();
        env.GetAssemblyName(Arg.Any<Idempotency>()).Returns(assemblyName);
        env.GetAssemblyVersion(Arg.Any<Idempotency>()).Returns(assemblyVersion);

        var conf = new PowertoolsConfigurations(new SystemWrapper(env));

        // Act
        var xRayRecorder = new Idempotency(conf);

        // Assert
        env.Received(1).SetEnvironmentVariable(
            "AWS_EXECUTION_ENV",
            $"{Constants.FeatureContextIdentifier}/Idempotency/{assemblyVersion}"
        );

        env.Received(1).GetEnvironmentVariable("AWS_EXECUTION_ENV");

        Assert.NotNull(xRayRecorder);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyOnSubMethodAnnotated_AndFirstCall_ShouldPutInStore()
    {
        // Arrange
        var store = Substitute.For<BasePersistenceStore>();
        Idempotency.Configure(builder => builder.WithPersistenceStore(store));

        var context = new TestLambdaContext
        {
            RemainingTime = TimeSpan.FromSeconds(30)
        };

        // Act
        IdempotencyInternalFunction function = new IdempotencyInternalFunction(true);
        Product product = new Product(42, "fake product", 12);
        Basket resultBasket = function.HandleRequest(product, context);

        // Assert
        resultBasket.Products.Count.Should().Be(2);
        function.IsSubMethodCalled.Should().BeTrue();

        await store
            .Received(1)
            .SaveInProgress(
                Arg.Is<JsonDocument>(t =>
                    t.ToString() == JsonSerializer.SerializeToDocument("fake", new JsonSerializerOptions())
                        .ToString()),
                Arg.Any<DateTimeOffset>(), Arg.Is<double>(d => d == 30000));

        await store
            .Received(1)
            .SaveSuccess(Arg.Any<JsonDocument>(), Arg.Is<Basket>(y => y.Equals(resultBasket)),
                Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void Handle_WhenIdempotencyOnSubMethodAnnotated_AndSecondCall_AndNotExpired_ShouldGetFromStore()
    {
        // Arrange
        var store = Substitute.For<BasePersistenceStore>();
        store.SaveInProgress(Arg.Any<JsonDocument>(), Arg.Any<DateTimeOffset>(), Arg.Any<double>())
            .Returns(_ => throw new IdempotencyItemAlreadyExistsException());

        Idempotency.Configure(builder => builder.WithPersistenceStore(store));

        var product = new Product(42, "fake product", 12);
        var basket = new Basket(product);
        var record = new DataRecord(
            "fake",
            DataRecord.DataRecordStatus.COMPLETED,
            DateTimeOffset.UtcNow.AddSeconds(356).ToUnixTimeSeconds(),
            JsonSerializer.SerializeToNode(basket)!.ToString(),
            null);
        store.GetRecord(Arg.Any<JsonDocument>(), Arg.Any<DateTimeOffset>())
            .Returns(record);

        // Act
        var function = new IdempotencyInternalFunction(true);
        Basket resultBasket = function.HandleRequest(product, new TestLambdaContext());

        // assert
        resultBasket.Should().Be(basket);
        function.IsSubMethodCalled.Should().BeFalse();
    }

    [Fact]
    public void Handle_WhenIdempotencyOnSubMethodAnnotated_AndKeyJMESPath_ShouldPutInStoreWithKey()
    {
        // Arrange
        var store = new InMemoryPersistenceStore();
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store)
#if NET8_0_OR_GREATER
                .WithJsonSerializationContext(TestJsonSerializerContext.Default)
#endif
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
        );

        // Act
        IdempotencyInternalFunctionInternalKey function = new IdempotencyInternalFunctionInternalKey();
        Product product = new Product(42, "fake product", 12);
        function.HandleRequest(product, new TestLambdaContext());

        // Assert
        // a1d0c6e83f027327d8461063f4ac58a6 = MD5(42)
        store.GetRecord("testFunction.createBasket#a1d0c6e83f027327d8461063f4ac58a6").Should().NotBeNull();
    }

    [Fact]
    public void Handle_WhenIdempotencyOnSubMethodNotAnnotated_ShouldThrowException()
    {
        // Arrange
        var store = Substitute.For<BasePersistenceStore>();
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store)
        );

        // Act
        IdempotencyInternalFunctionInvalid function = new IdempotencyInternalFunctionInvalid();
        Product product = new Product(42, "fake product", 12);
        Action act = () => function!.HandleRequest(product, new TestLambdaContext());

        // Assert
        act.Should().Throw<IdempotencyConfigurationException>();
    }

    [Fact]
    public void Handle_WhenIdempotencyOnSubMethodVoid_ShouldThrowException()
    {
        // Arrange
        var store = Substitute.For<BasePersistenceStore>();
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store)
        );

        // Act
        IdempotencyInternalFunctionVoid function = new IdempotencyInternalFunctionVoid();
        Product product = new Product(42, "fake product", 12);
        Action act = () => function.HandleRequest(product, new TestLambdaContext());

        // Assert
        act.Should().Throw<IdempotencyConfigurationException>();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Constants.IdempotencyDisabledEnv, "false");
    }
}