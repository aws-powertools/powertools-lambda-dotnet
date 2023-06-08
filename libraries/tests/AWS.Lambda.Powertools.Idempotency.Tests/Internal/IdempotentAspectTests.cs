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
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Idempotency.Tests.Handlers;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;
using FluentAssertions;
using Moq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
        var store = new Mock<BasePersistenceStore>();
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store.Object)
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
            );
        
        var function = Activator.CreateInstance(type) as IIdempotencyEnabledFunction;
        var product = new Product(42, "fake product", 12);
        
        //Act
        var basket = await function!.HandleTest(product, new TestLambdaContext());
        
        //Assert
        basket.Products.Count.Should().Be(1);
        function.HandlerExecuted.Should().BeTrue();

        store
            .Verify(x=>x.SaveInProgress(It.Is<JsonDocument>(t=> t.ToString() == JsonSerializer.SerializeToDocument(product, It.IsAny<JsonSerializerOptions>()).ToString()), It.IsAny<DateTimeOffset>()));

        store
            .Verify(x=>x.SaveSuccess(It.IsAny<JsonDocument>(), It.Is<Basket>(y => y.Equals(basket)), It.IsAny<DateTimeOffset>()));
    }

    [Theory]
    [InlineData(typeof(IdempotencyEnabledFunction))]
    [InlineData(typeof(IdempotencyEnabledSyncFunction))]
    public async Task Handle_WhenSecondCall_AndNotExpired_ShouldGetFromStore(Type type)
    {
        //Arrange
        var store = new Mock<BasePersistenceStore>();
        store.Setup(x=>x.SaveInProgress(It.IsAny<JsonDocument>(), It.IsAny<DateTimeOffset>()))
            .Throws<IdempotencyItemAlreadyExistsException>();
    
        // GIVEN
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store.Object)
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
        store.Setup(x=>x.GetRecord(It.IsAny<JsonDocument>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(record);
    
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
    public async Task Handle_WhenSecondCall_AndStatusInProgress_ShouldThrowIdempotencyAlreadyInProgressException(Type type)
    {
        // Arrange
        var store = new Mock<BasePersistenceStore>();
        
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store.Object)
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
            );
        store.Setup(x=>x.SaveInProgress(It.IsAny<JsonDocument>(), It.IsAny<DateTimeOffset>()))
            .Throws<IdempotencyItemAlreadyExistsException>();
    
        var product = new Product(42, "fake product", 12);
        var basket = new Basket(product);
        var record = new DataRecord(
            "42",
            DataRecord.DataRecordStatus.INPROGRESS,
            DateTimeOffset.UtcNow.AddSeconds(356).ToUnixTimeSeconds(),
            JsonSerializer.SerializeToNode(basket)!.ToString(),
            null);
        store.Setup(x=>x.GetRecord(It.IsAny<JsonDocument>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(record);
    
        // Act
        var function = Activator.CreateInstance(type) as IIdempotencyEnabledFunction;
        Func<Task> act = async () => await function!.HandleTest(product, new TestLambdaContext());
    
        // Assert
        await act.Should().ThrowAsync<IdempotencyAlreadyInProgressException>();
    }

    [Theory]
    [InlineData(typeof(IdempotencyWithErrorFunction))]
    [InlineData(typeof(IdempotencyWithErrorSyncFunction))]
    public async Task Handle_WhenThrowException_ShouldDeleteRecord_AndThrowFunctionException(Type type) 
    {
        // Arrange
        var store = new Mock<BasePersistenceStore>();
    
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store.Object)
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
            );
        
        var function = Activator.CreateInstance(type) as IIdempotencyWithErrorFunction;
        var product = new Product(42, "fake product", 12);
    
        // Act
        Func<Task> act = async () => await function!.HandleTest(product, new TestLambdaContext());
    
        // Assert
        await act.Should().ThrowAsync<IndexOutOfRangeException>();
        store.Verify(
            x => x.DeleteRecord(It.IsAny<JsonDocument>(), It.IsAny<IndexOutOfRangeException>()));
    }
    
    [Theory]
    [InlineData(typeof(IdempotencyEnabledFunction))]
    [InlineData(typeof(IdempotencyEnabledSyncFunction))]
    public async Task Handle_WhenIdempotencyDisabled_ShouldJustRunTheFunction(Type type)
    {
        
        // Arrange
        var store = new Mock<BasePersistenceStore>();
        
        Environment.SetEnvironmentVariable(Constants.IdempotencyDisabledEnv, "true");
        
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store.Object)
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
            );
        
        var function = Activator.CreateInstance(type) as IIdempotencyEnabledFunction;
        var product = new Product(42, "fake product", 12);
        
        // Act
        var basket = await function!.HandleTest(product, new TestLambdaContext());

        // Assert
        store.Invocations.Count.Should().Be(0);
        basket.Products.Count.Should().Be(1);
        function.HandlerExecuted.Should().BeTrue();
    }

    [Fact]
    public void Idempotency_Set_Execution_Environment_Context()
    {
        // Arrange
        var assemblyName = "AWS.Lambda.Powertools.Idempotency";
        var assemblyVersion = "1.0.0";
        
        var env = new Mock<IPowertoolsEnvironment>();
        env.Setup(x => x.GetAssemblyName(It.IsAny<Idempotency>())).Returns(assemblyName);
        env.Setup(x => x.GetAssemblyVersion(It.IsAny<Idempotency>())).Returns(assemblyVersion);

        var conf = new PowertoolsConfigurations(new SystemWrapper(env.Object));
        
        // Act
        var xRayRecorder = new Idempotency(conf);

        // Assert
        env.Verify(v =>
            v.SetEnvironmentVariable(
                "AWS_EXECUTION_ENV", $"{Constants.FeatureContextIdentifier}/Idempotency/{assemblyVersion}"
            ), Times.Once);
            
        env.Verify(v =>
            v.GetEnvironmentVariable(
                "AWS_EXECUTION_ENV"
            ), Times.Once);
        
        Assert.NotNull(xRayRecorder);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Constants.IdempotencyDisabledEnv, "false");
    }
}