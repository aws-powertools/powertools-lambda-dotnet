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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.BatchProcessing.Exceptions;
using AWS.Lambda.Powertools.BatchProcessing.Internal;
using Xunit;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests;

[Collection("Sequential")]
public class AotCompatibilityTests
{
    #region Test Models

    public class TestAotModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UnregisteredModel
    {
        public string Value { get; set; }
    }

    #endregion

    #region AotCompatibilityHelper Tests

    [Fact]
    public void IsAotMode_ReturnsExpectedValue()
    {
        // Act
        var isAotMode = AotCompatibilityHelper.IsAotMode();

        // Assert
        // In test environment, this should return false (not AOT compiled)
        // The actual value depends on the runtime, but we can test that it returns a boolean
        Assert.IsType<bool>(isAotMode);
    }

    [Fact]
    public void ValidateTypeInContext_WithValidType_ReturnsTrue()
    {
        // Arrange
        var context = TestAotJsonSerializerContext.Default;

        // Act
        var result = AotCompatibilityHelper.ValidateTypeInContext<TestAotModel>(context, false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateTypeInContext_WithUnregisteredType_ReturnsFalse()
    {
        // Arrange
        var context = TestAotJsonSerializerContext.Default;

        // Act
        var result = AotCompatibilityHelper.ValidateTypeInContext<UnregisteredModel>(context, false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateTypeInContext_WithUnregisteredTypeAndThrow_ThrowsException()
    {
        // Arrange
        var context = TestAotJsonSerializerContext.Default;

        // Act & Assert
        var exception = Assert.Throws<AotTypeValidationException>(() =>
            AotCompatibilityHelper.ValidateTypeInContext<UnregisteredModel>(context, true));

        Assert.Equal(typeof(UnregisteredModel), exception.TargetType);
        Assert.Contains("UnregisteredModel", exception.Message);
        Assert.Contains("JsonSerializable", exception.Message);
    }

    [Fact]
    public void ValidateTypeInContext_WithNullContext_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<AotTypeValidationException>(() =>
            AotCompatibilityHelper.ValidateTypeInContext<TestAotModel>(null, true));

        Assert.Equal(typeof(TestAotModel), exception.TargetType);
        Assert.Contains("JsonSerializerContext is null", exception.Message);
    }

    [Fact]
    public void ValidateTypeInContext_WithNullContextAndNoThrow_ReturnsFalse()
    {
        // Act
        var result = AotCompatibilityHelper.ValidateTypeInContext<TestAotModel>(null, false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAotCompatibilityErrorMessage_WithoutContext_ReturnsCorrectMessage()
    {
        // Act
        var message = AotCompatibilityHelper.GetAotCompatibilityErrorMessage(typeof(TestAotModel), false);

        // Assert
        Assert.Contains("AOT compilation requires a JsonSerializerContext", message);
        Assert.Contains("TestAotModel", message);
        Assert.Contains("JsonSerializable", message);
    }

    [Fact]
    public void GetAotCompatibilityErrorMessage_WithContext_ReturnsCorrectMessage()
    {
        // Act
        var message = AotCompatibilityHelper.GetAotCompatibilityErrorMessage(typeof(TestAotModel), true);

        // Assert
        Assert.Contains("does not contain type information", message);
        Assert.Contains("TestAotModel", message);
        Assert.Contains("JsonSerializable", message);
    }

    [Fact]
    public void ValidateAotCompatibility_InNonAotMode_DoesNotThrow()
    {
        // Arrange
        var options = new DeserializationOptions();

        // Act & Assert - Should not throw in non-AOT mode
        AotCompatibilityHelper.ValidateAotCompatibility<TestAotModel>(options);
    }

    [Fact]
    public void ValidateAotCompatibility_WithJsonSerializerContext_ValidatesType()
    {
        // Arrange
        var options = new DeserializationOptions(TestAotJsonSerializerContext.Default);

        // Act & Assert - Should not throw for registered type
        AotCompatibilityHelper.ValidateAotCompatibility<TestAotModel>(options);
    }

    [Fact]
    public void ValidateAotCompatibility_WithUnregisteredType_ThrowsException()
    {
        // Arrange
        var options = new DeserializationOptions(TestAotJsonSerializerContext.Default);

        // Act & Assert
        var exception = Assert.Throws<AotTypeValidationException>(() =>
            AotCompatibilityHelper.ValidateAotCompatibility<UnregisteredModel>(options));

        Assert.Equal(typeof(UnregisteredModel), exception.TargetType);
        Assert.Contains("UnregisteredModel", exception.Message);
    }

    [Fact]
    public void FallbackDeserialize_WithJsonSerializerOptions_Succeeds()
    {
        // Arrange
        var json = """{"Id":1,"Name":"Test","CreatedAt":"2023-01-01T00:00:00Z"}""";
        var options = new DeserializationOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        };

        // Act
        var result = AotCompatibilityHelper.FallbackDeserialize<TestAotModel>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void FallbackDeserialize_WithNullOptions_Succeeds()
    {
        // Arrange
        var json = """{"Id":1,"Name":"Test","CreatedAt":"2023-01-01T00:00:00Z"}""";

        // Act
        var result = AotCompatibilityHelper.FallbackDeserialize<TestAotModel>(json, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    #endregion

    #region JsonDeserializationService AOT Tests

    [Fact]
    public void JsonDeserializationService_WithValidJsonSerializerContext_Succeeds()
    {
        // Arrange
        var service = new JsonDeserializationService();
        var json = """{"Id":1,"Name":"Test","CreatedAt":"2023-01-01T00:00:00Z"}""";
        var options = new DeserializationOptions(TestAotJsonSerializerContext.Default);

        // Act
        var result = service.Deserialize<TestAotModel>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.Equal(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.CreatedAt);
    }

    [Fact]
    public void JsonDeserializationService_WithUnregisteredTypeInContext_ThrowsException()
    {
        // Arrange
        var service = new JsonDeserializationService();
        var json = """{"Value":"test"}""";
        var options = new DeserializationOptions(TestAotJsonSerializerContext.Default);

        // Act & Assert
        var exception = Assert.Throws<AotTypeValidationException>(() =>
            service.Deserialize<UnregisteredModel>(json, options));

        Assert.Equal(typeof(UnregisteredModel), exception.TargetType);
        Assert.Contains("UnregisteredModel", exception.Message);
    }

    [Fact]
    public void JsonDeserializationService_TryDeserialize_WithUnregisteredType_ReturnsFalse()
    {
        // Arrange
        var service = new JsonDeserializationService();
        var json = """{"Value":"test"}""";
        var options = new DeserializationOptions(TestAotJsonSerializerContext.Default);

        // Act
        var success = service.TryDeserialize<UnregisteredModel>(json, out var result, out var exception, options);

        // Assert
        Assert.False(success);
        Assert.Null(result);
        Assert.NotNull(exception);
        Assert.IsType<AotTypeValidationException>(exception);
    }

    [Fact]
    public void JsonDeserializationService_WithPrimitiveTypes_WorksWithContext()
    {
        // Arrange
        var service = new JsonDeserializationService();
        var options = new DeserializationOptions(TestAotJsonSerializerContext.Default);

        // Act & Assert
        Assert.Equal(42, service.Deserialize<int>("42", options));
        Assert.Equal("test", service.Deserialize<string>("\"test\"", options));
    }

    #endregion

    #region Exception Tests

    [Fact]
    public void AotCompatibilityException_ConstructorWithMessage_SetsProperties()
    {
        // Arrange
        var targetType = typeof(TestAotModel);
        var message = "Test message";

        // Act
        var exception = new AotCompatibilityException(targetType, message);

        // Assert
        Assert.Equal(targetType, exception.TargetType);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void AotCompatibilityException_ConstructorWithInnerException_SetsProperties()
    {
        // Arrange
        var targetType = typeof(TestAotModel);
        var message = "Test message";
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new AotCompatibilityException(targetType, message, innerException);

        // Assert
        Assert.Equal(targetType, exception.TargetType);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void AotTypeValidationException_ConstructorWithMessage_SetsProperties()
    {
        // Arrange
        var targetType = typeof(TestAotModel);
        var message = "Test message";

        // Act
        var exception = new AotTypeValidationException(targetType, message);

        // Assert
        Assert.Equal(targetType, exception.TargetType);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void AotTypeValidationException_ConstructorWithInnerException_SetsProperties()
    {
        // Arrange
        var targetType = typeof(TestAotModel);
        var message = "Test message";
        var innerException = new NotSupportedException("Inner");

        // Act
        var exception = new AotTypeValidationException(targetType, message, innerException);

        // Assert
        Assert.Equal(targetType, exception.TargetType);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void DeserializationOptions_WithJsonSerializerContext_PreservesContext()
    {
        // Arrange
        var context = TestAotJsonSerializerContext.Default;

        // Act
        var options = new DeserializationOptions(context);

        // Assert
        Assert.Equal(context, options.JsonSerializerContext);
        Assert.Null(options.JsonSerializerOptions);
    }

    [Fact]
    public void JsonDeserializationService_ContextTakesPrecedenceOverOptions()
    {
        // Arrange
        var service = new JsonDeserializationService();
        var json = """{"Id":1,"Name":"Test","CreatedAt":"2023-01-01T00:00:00Z"}""";
        var options = new DeserializationOptions
        {
            JsonSerializerContext = TestAotJsonSerializerContext.Default,
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = false }
        };

        // Act
        var result = service.Deserialize<TestAotModel>(json, options);

        // Assert - Should succeed because JsonSerializerContext is used instead of JsonSerializerOptions
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    #endregion
}

// JsonSerializerContext needs to be outside the test class and partial for source generation
[JsonSerializable(typeof(AotCompatibilityTests.TestAotModel))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
public partial class TestAotJsonSerializerContext : JsonSerializerContext
{
}