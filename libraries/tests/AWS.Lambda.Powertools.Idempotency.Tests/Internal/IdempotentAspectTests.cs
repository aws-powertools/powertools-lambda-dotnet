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
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Idempotency.Tests.Handlers;
using AWS.Lambda.Powertools.Idempotency.Tests.Model;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Internal;

public class IdempotentAspectTests
{
    [Fact]
    public async Task Handle_WhenFirstCall_ShouldPutInStore()
    {
        //Arrange
        var store = new Mock<BasePersistenceStore>();
        Idempotency.Config()
            .WithPersistenceStore(store.Object)
            .WithConfig(IdempotencyConfig.Builder()
                .WithEventKeyJmesPath("Id")
                .Build()
            ).Configure();
        
        IdempotencyEnabledFunction function = new IdempotencyEnabledFunction();
        Product product = new Product(42, "fake product", 12);
        
        //Act
        Basket basket = await function.Handle(product, new TestLambdaContext());
        
        //Assert
        basket.Products.Count.Should().Be(1);
        function.HandlerExecuted.Should().BeTrue();

        store
            .Verify(x=>x.SaveInProgress(It.Is<JToken>(t=> t.ToString() == JToken.FromObject(product).ToString()), It.IsAny<DateTimeOffset>()));

        store
            .Verify(x=>x.SaveSuccess(It.IsAny<JToken>(), It.Is<Basket>(y => y.Equals(basket)), It.IsAny<DateTimeOffset>()));
    }

    [Fact]
    public async Task Handle_WhenSecondCall_AndNotExpired_ShouldGetFromStore()
    {
        //Arrange
        var store = new Mock<BasePersistenceStore>();
        store.Setup(x=>x.SaveInProgress(It.IsAny<JToken>(), It.IsAny<DateTimeOffset>()))
            .Throws<IdempotencyItemAlreadyExistsException>();

        // GIVEN
        Idempotency.Config()
            .WithPersistenceStore(store.Object)
            .WithConfig(IdempotencyConfig.Builder()
                .WithEventKeyJmesPath("Id")
                .Build()
            ).Configure();

        Product product = new Product(42, "fake product", 12);
        Basket basket = new Basket(product);
        DataRecord record = new DataRecord(
            "42",
            DataRecord.DataRecordStatus.COMPLETED,
            DateTimeOffset.UtcNow.AddSeconds(356).ToUnixTimeSeconds(),
            JToken.FromObject(basket).ToString(),
            null);
        store.Setup(x=>x.GetRecord(It.IsAny<JToken>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(record);

        IdempotencyEnabledFunction function = new IdempotencyEnabledFunction();

        // Act
        Basket resultBasket = await function.Handle(product, new TestLambdaContext());

        // Assert
        resultBasket.Should().Be(basket);
        function.HandlerExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSecondCall_AndStatusInProgress_ShouldThrowIdempotencyAlreadyInProgressException()
    {
        // Arrange
        var store = new Mock<BasePersistenceStore>();
        
        Idempotency.Config()
            .WithPersistenceStore(store.Object)
            .WithConfig(IdempotencyConfig.Builder()
                .WithEventKeyJmesPath("Id")
                .Build()
            ).Configure();
        store.Setup(x=>x.SaveInProgress(It.IsAny<JToken>(), It.IsAny<DateTimeOffset>()))
            .Throws<IdempotencyItemAlreadyExistsException>();

        Product product = new Product(42, "fake product", 12);
        Basket basket = new Basket(product);
        DataRecord record = new DataRecord(
            "42",
            DataRecord.DataRecordStatus.INPROGRESS,
            DateTimeOffset.UtcNow.AddSeconds(356).ToUnixTimeSeconds(),
            JToken.FromObject(basket).ToString(),
            null);
        store.Setup(x=>x.GetRecord(It.IsAny<JToken>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(record);

        // Act
        IdempotencyEnabledFunction function = new IdempotencyEnabledFunction();
        Func<Task> act = async () => await function.Handle(product, new TestLambdaContext());

        // Assert
        await act.Should().ThrowAsync<IdempotencyAlreadyInProgressException>();
    }
    
    [Fact]
    public async Task Handle_WhenThrowException_ShouldDeleteRecord_AndThrowFunctionException() 
    {
        // Arrange
        var store = new Mock<BasePersistenceStore>();

        Idempotency.Config()
            .WithPersistenceStore(store.Object)
            .WithConfig(IdempotencyConfig.Builder()
                .WithEventKeyJmesPath("Id")
                .Build()
            ).Configure();
        
        IdempotencyWithErrorFunction function = new IdempotencyWithErrorFunction();
        Product product = new Product(42, "fake product", 12);

        // Act
        Func<Task> act = async () => await function.Handle(product, new TestLambdaContext());

        // Assert
        await act.Should().ThrowAsync<IndexOutOfRangeException>();
        store.Verify(
            x => x.DeleteRecord(It.IsAny<JToken>(), It.IsAny<IndexOutOfRangeException>()));
    }

    [Fact]
    public async Task Handle_WhenIdempotencyDisabled_ShouldJustRunTheFunction()
    {
        try
        {
            // Arrange
            var store = new Mock<BasePersistenceStore>();
            Environment.SetEnvironmentVariable(Constants.IDEMPOTENCY_DISABLED_ENV, "true");
            
            Idempotency.Config()
                .WithPersistenceStore(store.Object)
                .WithConfig(IdempotencyConfig.Builder()
                    .WithEventKeyJmesPath("Id")
                    .Build()
                ).Configure();
            
            IdempotencyEnabledFunction function = new IdempotencyEnabledFunction();
            Product product = new Product(42, "fake product", 12);
            
            // Act
            Basket basket = await function.Handle(product, new TestLambdaContext());

            // Assert
            store.Invocations.Count.Should().Be(0);
            basket.Products.Count.Should().Be(1);
            function.HandlerExecuted.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Constants.IDEMPOTENCY_DISABLED_ENV, "false");
        }
    }
}