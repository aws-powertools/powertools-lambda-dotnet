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
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

[Collection("Sequential")]
public partial class JsonDeserializationServiceTests
{
    private readonly JsonDeserializationService _service;

    public JsonDeserializationServiceTests()
    {
        _service = new JsonDeserializationService();
    }

    #region Test Models

    public class TestProduct
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class TestOrder
    {
        public string OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public TestProduct[] Items { get; set; }
    }

    [JsonSerializable(typeof(TestProduct))]
    [JsonSerializable(typeof(TestOrder))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(int))]
    public partial class TestJsonSerializerContext : JsonSerializerContext
    {
    }

    #endregion

    #region Deserialize Tests

    [Fact]
    public void Deserialize_ValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var json = """{"Id":1,"Name":"Test Product","Price":99.99}""";

        // Act
        var result = _service.Deserialize<TestProduct>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal(99.99m, result.Price);
    }

    [Fact]
    public void Deserialize_WithJsonSerializerOptions_ReturnsDeserializedObject()
    {
        // Arrange
        var json = """{"id":1,"name":"Test Product","price":99.99}""";
        var options = new DeserializationOptions(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act
        var result = _service.Deserialize<TestProduct>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal(99.99m, result.Price);
    }

    [Fact]
    public void Deserialize_WithJsonSerializerContext_ReturnsDeserializedObject()
    {
        // Arrange
        var json = """{"Id":1,"Name":"Test Product","Price":99.99}""";
        var options = new DeserializationOptions(TestJsonSerializerContext.Default);

        // Act
        var result = _service.Deserialize<TestProduct>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal(99.99m, result.Price);
    }

    [Fact]
    public void Deserialize_ComplexObject_ReturnsDeserializedObject()
    {
        // Arrange
        var json = """{"OrderId":"ORD-123","OrderDate":"2023-01-01T00:00:00Z","Items":[{"Id":1,"Name":"Product 1","Price":10.00},{"Id":2,"Name":"Product 2","Price":20.00}]}""";

        // Act
        var result = _service.Deserialize<TestOrder>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ORD-123", result.OrderId);
        Assert.Equal(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.OrderDate);
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items.Length);
        Assert.Equal("Product 1", result.Items[0].Name);
        Assert.Equal("Product 2", result.Items[1].Name);
    }

    [Fact]
    public void Deserialize_PrimitiveTypes_ReturnsDeserializedValues()
    {
        // Arrange & Act & Assert
        Assert.Equal(42, _service.Deserialize<int>("42"));
        Assert.Equal("test", _service.Deserialize<string>("\"test\""));
        Assert.True(_service.Deserialize<bool>("true"));
        Assert.Equal(3.14, _service.Deserialize<double>("3.14"));
    }

    [Fact]
    public void Deserialize_NullData_ThrowsDeserializationException()
    {
        // Act & Assert
        var exception = Assert.Throws<DeserializationException>(() => _service.Deserialize<TestProduct>(null));
        Assert.Contains("Data cannot be null or empty", exception.Message);
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void Deserialize_EmptyData_ThrowsDeserializationException()
    {
        // Act & Assert
        var exception = Assert.Throws<DeserializationException>(() => _service.Deserialize<TestProduct>(""));
        Assert.Contains("Data cannot be null or empty", exception.Message);
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsDeserializationException()
    {
        // Arrange
        var invalidJson = "{invalid json}";

        // Act & Assert
        var exception = Assert.Throws<DeserializationException>(() => _service.Deserialize<TestProduct>(invalidJson));
        Assert.Equal(invalidJson, exception.RecordData);
        Assert.Equal(typeof(TestProduct), exception.TargetType);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void Deserialize_InvalidJsonWithIgnoreErrors_ReturnsDefault()
    {
        // Arrange
        var invalidJson = "{invalid json}";
        var options = new DeserializationOptions { IgnoreDeserializationErrors = true };

        // Act
        var result = _service.Deserialize<TestProduct>(invalidJson, options);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region TryDeserialize Tests

    [Fact]
    public void TryDeserialize_ValidJson_ReturnsTrue()
    {
        // Arrange
        var json = """{"Id":1,"Name":"Test Product","Price":99.99}""";

        // Act
        var success = _service.TryDeserialize<TestProduct>(json, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal(99.99m, result.Price);
    }

    [Fact]
    public void TryDeserialize_InvalidJson_ReturnsFalse()
    {
        // Arrange
        var invalidJson = "{invalid json}";

        // Act
        var success = _service.TryDeserialize<TestProduct>(invalidJson, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryDeserialize_NullData_ReturnsFalse()
    {
        // Act
        var success = _service.TryDeserialize<TestProduct>(null, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryDeserialize_WithException_ReturnsFalseAndException()
    {
        // Arrange
        var invalidJson = "{invalid json}";

        // Act
        var success = _service.TryDeserialize<TestProduct>(invalidJson, out var result, out var exception);

        // Assert
        Assert.False(success);
        Assert.Null(result);
        Assert.NotNull(exception);
        Assert.IsType<JsonException>(exception);
    }

    [Fact]
    public void TryDeserialize_WithJsonSerializerContext_ReturnsTrue()
    {
        // Arrange
        var json = """{"Id":1,"Name":"Test Product","Price":99.99}""";
        var options = new DeserializationOptions(TestJsonSerializerContext.Default);

        // Act
        var success = _service.TryDeserialize<TestProduct>(json, out var result, options);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    #endregion

    #region DeserializationOptions Tests

    [Fact]
    public void DeserializationOptions_DefaultConstructor_SetsDefaults()
    {
        // Act
        var options = new DeserializationOptions();

        // Assert
        Assert.Null(options.JsonSerializerContext);
        Assert.Null(options.JsonSerializerOptions);
        Assert.False(options.IgnoreDeserializationErrors);
    }

    [Fact]
    public void DeserializationOptions_JsonSerializerContextConstructor_SetsContext()
    {
        // Arrange
        var context = TestJsonSerializerContext.Default;

        // Act
        var options = new DeserializationOptions(context);

        // Assert
        Assert.Equal(context, options.JsonSerializerContext);
        Assert.Null(options.JsonSerializerOptions);
        Assert.False(options.IgnoreDeserializationErrors);
    }

    [Fact]
    public void DeserializationOptions_JsonSerializerOptionsConstructor_SetsOptions()
    {
        // Arrange
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var options = new DeserializationOptions(jsonOptions);

        // Assert
        Assert.Null(options.JsonSerializerContext);
        Assert.Equal(jsonOptions, options.JsonSerializerOptions);
        Assert.False(options.IgnoreDeserializationErrors);
    }

    #endregion

    #region Singleton Tests

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        // Act
        var instance1 = JsonDeserializationService.Instance;
        var instance2 = JsonDeserializationService.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Instance_IsNotNull()
    {
        // Act
        var instance = JsonDeserializationService.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Deserialize_JsonSerializerContextTakesPrecedence_OverJsonSerializerOptions()
    {
        // Arrange
        var json = """{"Id":1,"Name":"Test Product","Price":99.99}""";
        var options = new DeserializationOptions
        {
            JsonSerializerContext = TestJsonSerializerContext.Default,
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = false }
        };

        // Act
        var result = _service.Deserialize<TestProduct>(json, options);

        // Assert - Should succeed because JsonSerializerContext is used instead of JsonSerializerOptions
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Deserialize_WhitespaceOnlyData_ThrowsDeserializationException()
    {
        // Act & Assert
        var exception = Assert.Throws<DeserializationException>(() => _service.Deserialize<TestProduct>("   "));
        Assert.Contains("Data cannot be null or empty", exception.Message);
    }

    #endregion
}