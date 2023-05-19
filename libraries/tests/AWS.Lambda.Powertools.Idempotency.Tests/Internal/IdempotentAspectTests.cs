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
public class IdempotentAspectTests
{
    [Fact]
    public async Task Handle_WhenFirstCall_ShouldPutInStore()
    {
        //Arrange
        var store = new Mock<BasePersistenceStore>();
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store.Object)
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
            );
        
        var function = new IdempotencyEnabledFunction();
        var product = new Product(42, "fake product", 12);
        
        //Act
        var basket = await function.Handle(product, new TestLambdaContext());
        
        //Assert
        basket.Products.Count.Should().Be(1);
        function.HandlerExecuted.Should().BeTrue();

        store
            .Verify(x=>x.SaveInProgress(It.Is<JsonDocument>(t=> t.ToString() == JsonSerializer.SerializeToDocument(product, It.IsAny<JsonSerializerOptions>()).ToString()), It.IsAny<DateTimeOffset>()));

        store
            .Verify(x=>x.SaveSuccess(It.IsAny<JsonDocument>(), It.Is<Basket>(y => y.Equals(basket)), It.IsAny<DateTimeOffset>()));
    }

    [Fact]
    public async Task Handle_WhenSecondCall_AndNotExpired_ShouldGetFromStore()
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
    
        var function = new IdempotencyEnabledFunction();
    
        // Act
        var resultBasket = await function.Handle(product, new TestLambdaContext());
    
        // Assert
        resultBasket.Should().Be(basket);
        function.HandlerExecuted.Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_WhenSecondCall_AndStatusInProgress_ShouldThrowIdempotencyAlreadyInProgressException()
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
        var function = new IdempotencyEnabledFunction();
        Func<Task> act = async () => await function.Handle(product, new TestLambdaContext());
    
        // Assert
        await act.Should().ThrowAsync<IdempotencyAlreadyInProgressException>();
    }

    [Fact]
    public async Task Handle_WhenThrowException_ShouldDeleteRecord_AndThrowFunctionException() 
    {
        // Arrange
        var store = new Mock<BasePersistenceStore>();
    
        Idempotency.Configure(builder =>
            builder
                .WithPersistenceStore(store.Object)
                .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
            );
        
        var function = new IdempotencyWithErrorFunction();
        var product = new Product(42, "fake product", 12);
    
        // Act
        Func<Task> act = async () => await function.Handle(product, new TestLambdaContext());
    
        // Assert
        await act.Should().ThrowAsync<IndexOutOfRangeException>();
        store.Verify(
            x => x.DeleteRecord(It.IsAny<JsonDocument>(), It.IsAny<IndexOutOfRangeException>()));
    }
    
    [Fact]
    public async Task Handle_WhenIdempotencyDisabled_ShouldJustRunTheFunction()
    {
        try
        {
            // Arrange
            var store = new Mock<BasePersistenceStore>();
            
            Environment.SetEnvironmentVariable(Constants.IdempotencyDisabledEnv, "true");
            
            Idempotency.Configure(builder =>
                builder
                    .WithPersistenceStore(store.Object)
                    .WithOptions(optionsBuilder => optionsBuilder.WithEventKeyJmesPath("Id"))
                );
            
            var function = new IdempotencyEnabledFunction();
            var product = new Product(42, "fake product", 12);
            
            // Act
            var basket = await function.Handle(product, new TestLambdaContext());
    
            // Assert
            store.Invocations.Count.Should().Be(0);
            basket.Products.Count.Should().Be(1);
            function.HandlerExecuted.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Constants.IdempotencyDisabledEnv, "false");
        }
    }
}