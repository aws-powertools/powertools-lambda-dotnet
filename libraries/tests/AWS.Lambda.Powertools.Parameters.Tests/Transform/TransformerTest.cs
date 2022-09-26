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

using System.Text;
using AWS.Lambda.Powertools.Parameters.Transform;
using System.Text.Json;
using AWS.Lambda.Powertools.Parameters.Internal.Transform;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.Transform;

public class TransformerTest
{
    [Fact]
    public void Base64Transformer_TransformToString_ConvertFromBase64()
    {
        // Arrange
        var value = Guid.NewGuid().ToString();
        var plainTextBytes = Encoding.UTF8.GetBytes(value);
        var convertedValue = Convert.ToBase64String(plainTextBytes);
        
        var transformer = new Base64Transformer();

        // Act
        var result = transformer.Transform<string>(convertedValue);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }
    
    [Fact]
    public void Base64Transformer_TransformToObject_ReturnsNull()
    {
        // Arrange
        var value = Guid.NewGuid().ToString();
        var plainTextBytes = Encoding.UTF8.GetBytes(value);
        var convertedValue = Convert.ToBase64String(plainTextBytes);
        
        var transformer = new Base64Transformer();

        // Act
        var result = transformer.Transform<object>(convertedValue);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void Base64Transformer_TransformToNonString_ReturnsNull()
    {
        // Arrange
        var value = Guid.NewGuid().ToString();
        var plainTextBytes = Encoding.UTF8.GetBytes(value);
        var convertedValue = Convert.ToBase64String(plainTextBytes);
        
        var transformer = new Base64Transformer();

        // Act
        var result = transformer.Transform<List<int>>(convertedValue);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void JsonTransformer_TransformToType_ConvertFromJsonString()
    {
        // Arrange
        var keyValueMap = new Dictionary<string, List<string>>();

        var key1 = Guid.NewGuid().ToString();
        var value1 = new List<string>
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };
        keyValueMap.Add(key1, value1);

        var key2 = Guid.NewGuid().ToString();
        var value2 = new List<string>
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };
        keyValueMap.Add(key2, value2);

        var valueStr = JsonSerializer.Serialize(keyValueMap);

        var transformer = new JsonTransformer();

        // Act
        var result = transformer.Transform<Dictionary<string, List<string>>>(valueStr);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(key1, result?.First().Key);
        Assert.Equal(key2, result?.Last().Key);
        Assert.Equal(value1.First(), result?.First().Value.First());
        Assert.Equal(value1.Last(), result?.First().Value.Last());
        Assert.Equal(value2.First(), result?.Last().Value.First());
        Assert.Equal(value2.Last(), result?.Last().Value.Last());
    }
    
    [Fact]
    public void JsonTransformer_TransformToObject_ConvertFromJsonString()
    {
        // Arrange
        var keyValueMap = new Dictionary<string, List<string>>();

        var key1 = Guid.NewGuid().ToString();
        var value1 = new List<string>
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };
        keyValueMap.Add(key1, value1);

        var key2 = Guid.NewGuid().ToString();
        var value2 = new List<string>
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };
        keyValueMap.Add(key2, value2);

        var valueStr = JsonSerializer.Serialize(keyValueMap);

        var transformer = new JsonTransformer();

        // Act
        var result = transformer.Transform<object>(valueStr);

        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public void JsonTransformer_TransformToString_ReturnsJsonString()
    {
        // Arrange
        var keyValueMap = new Dictionary<string, List<string>>();

        var key1 = Guid.NewGuid().ToString();
        var value1 = new List<string>
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };
        keyValueMap.Add(key1, value1);

        var key2 = Guid.NewGuid().ToString();
        var value2 = new List<string>
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };
        keyValueMap.Add(key2, value2);

        var valueStr = JsonSerializer.Serialize(keyValueMap);

        var transformer = new JsonTransformer();

        // Act
        var result = transformer.Transform<string>(valueStr);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result, valueStr);
    }
}