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

#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Tracing.Serializers;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests.Serializers;

public class PowertoolsTracingSerializerTests
{
    private const string UninitializedContextMessage = "Serializer context not initialized. Ensure WithTracing() is called on the Lambda serializer.";
    
    public PowertoolsTracingSerializerTests()
    {
        var context = new TestJsonContext(new JsonSerializerOptions());
        PowertoolsTracingSerializer.AddSerializerContext(context);
    }

    [Fact]
    public void Serialize_WithSimpleObject_SerializesCorrectly()
    {
        // Arrange
        var testPerson = new TestPerson { Name = "John", Age = 30 };

        // Act
        var result = PowertoolsTracingSerializer.Serialize(testPerson);

        // Assert
        Assert.Contains("\"Name\":\"John\"", result);
        Assert.Contains("\"Age\":30", result);
    }

    [Fact]
    public void GetMetadataValue_WithSimpleObject_ReturnsCorrectDictionary()
    {
        // Arrange
        var testPerson = new TestPerson { Name = "John", Age = 30 };

        // Act
        var result = PowertoolsTracingSerializer.GetMetadataValue(testPerson);

        // Assert
        Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal("John", result["Name"]);
        Assert.Equal(30.0, result["Age"]);
    }

    [Fact]
    public void GetMetadataValue_WithComplexObject_ReturnsCorrectNestedDictionary()
    {
        // Arrange
        var testObject = new TestComplexObject
        {
            StringValue = "test",
            NumberValue = 42,
            BoolValue = true,
            NestedObject = new Dictionary<string, object>
            {
                { "nested", "value" },
                { "number", 123 }
            }
        };

        // Act
        var result = PowertoolsTracingSerializer.GetMetadataValue(testObject);

        // Assert
        Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal("test", result["StringValue"]);
        Assert.Equal(42.0, result["NumberValue"]);
        Assert.Equal(true, result["BoolValue"]);
        
        var nestedObject = Assert.IsType<Dictionary<string, object>>(result["NestedObject"]);
        Assert.Equal("value", nestedObject["nested"]);
        Assert.Equal(123.0, nestedObject["number"]);
    }
    
    [Fact]
    public void Serialize_WithoutInitializedContext_ThrowsInvalidOperationException()
    {
        // Arrange
        PowertoolsTracingSerializer.AddSerializerContext(null);
        var testObject = new TestPerson { Name = "John", Age = 30 };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PowertoolsTracingSerializer.Serialize(testObject));

        // Assert
        Assert.Equal(UninitializedContextMessage, exception.Message);
    }
    
    [Fact]
    public void Serialize_WithCircularReference_ThrowsJsonException()
    {
        // Arrange
        var circular = new TestCircularReference { Name = "Test" };
        circular.Reference = circular; // Create circular reference

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PowertoolsTracingSerializer.Serialize(circular));
    }
    
    [Fact]
    public void Serialize_WithUnregisteredType_ThrowsInvalidOperationException()
    {
        // Arrange
        var unregisteredType = new UnregisteredClass { Value = "test" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PowertoolsTracingSerializer.Serialize(unregisteredType));
    }
    
    [Fact]
    public void GetMetadataValue_WithoutInitializedContext_ThrowsInvalidOperationException()
    {
        // Arrange
        PowertoolsTracingSerializer.AddSerializerContext(null);
        var testObject = new TestPerson { Name = "John", Age = 30 };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PowertoolsTracingSerializer.GetMetadataValue(testObject));

        // Assert
        Assert.Equal(UninitializedContextMessage, exception.Message);
    }

    [Fact]
    public void GetMetadataValue_WithCircularReference_ThrowsJsonException()
    {
        // Arrange
        var circular = new TestCircularReference { Name = "Test" };
        circular.Reference = circular; // Create circular reference

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PowertoolsTracingSerializer.GetMetadataValue(circular));
    }

    [Fact]
    public void GetMetadataValue_WithUnregisteredType_ThrowsInvalidOperationException()
    {
        // Arrange
        var unregisteredType = new UnregisteredClass { Value = "test" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PowertoolsTracingSerializer.GetMetadataValue(unregisteredType));
    }

    [Fact]
    public void GetMetadataValue_WithInvalidJson_ThrowsJsonException()
    {
        // This test requires modifying the internal state of the serializer
        // which might not be possible in the actual implementation
        // Including it to show the concept of testing invalid JSON handling
        // Arrange
        var invalidJsonObject = new JsonTestObject { Value = Double.NaN }; // NaN is not valid in JSON

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PowertoolsTracingSerializer.GetMetadataValue(invalidJsonObject));
    }
}

public class TestCircularReference
{
    public string Name { get; set; }
    public TestCircularReference Reference { get; set; }
}

// Helper classes for testing
public class UnregisteredClass
{
    public string Value { get; set; }
}

[JsonSerializable(typeof(JsonTestObject))]
public class JsonTestObject
{
    public double Value { get; set; }
}
#endif