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

using AWS.Lambda.Powertools.Parameters.Transform;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.Transform;

public class TransformerManagerTest
{
    [Fact]
    public void GetTransformer_WhenJsonTransformation_ReturnsJsonTransformer()
    {
        // Arrange
        var transformerManager = new TransformerManager();
        
        // Act
        var result = transformerManager.GetTransformer(Transformation.Json);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonTransformer>(result);
    }
    
    [Fact]
    public void GetTransformer_WhenBase64Transformation_ReturnsBase64Transformer()
    {
        // Arrange
        var transformerManager = new TransformerManager();
        
        // Act
        var result = transformerManager.GetTransformer(Transformation.Base64);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Base64Transformer>(result);
    }
    
    [Fact]
    public void GetTransformer_WhenAutoTransformation_RaiseException()
    {
        // Arrange
        var transformerManager = new TransformerManager();

        // Act
        object Act() => transformerManager.GetTransformer(Transformation.Auto);

        // Assert
        Assert.Throws<NotSupportedException>(Act);
    }
    
    [Fact]
    public void TryGetTransformer_WhenJsonTransformation_ReturnsJsonTransformer()
    {
        // Arrange
        var transformerManager = new TransformerManager();
        
        // Act
        var result = transformerManager.TryGetTransformer(Transformation.Json, string.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonTransformer>(result);
    }
    
    [Fact]
    public void TryGetTransformer_WhenBase64Transformation_ReturnsBase64Transformer()
    {
        // Arrange
        var transformerManager = new TransformerManager();
        
        // Act
        var result = transformerManager.TryGetTransformer(Transformation.Base64, string.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Base64Transformer>(result);
    }
    
    [Fact]
    public void TryGetTransformer_WhenAutoTransformationAndJsonPostfix_ReturnsJsonTransformer()
    {
        // Arrange
        var transformerManager = new TransformerManager();
        
        // Act
        var result = transformerManager.TryGetTransformer(Transformation.Auto, "test.json");

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonTransformer>(result);
    }
    
    [Fact]
    public void TryGetTransformer_WhenAutoTransformationAndBase64Postfix_ReturnsBase64Transformer()
    {
        // Arrange
        var transformerManager = new TransformerManager();
        
        // Act
        var result = transformerManager.TryGetTransformer(Transformation.Auto, "test.base64");

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Base64Transformer>(result);
    }
    
    [Fact]
    public void TryGetTransformer_WhenAutoTransformationAndBinaryPostfix_ReturnsBase64Transformer()
    {
        // Arrange
        var transformerManager = new TransformerManager();
        
        // Act
        var result = transformerManager.TryGetTransformer(Transformation.Auto, "test.binary");

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Base64Transformer>(result);
    }
    
    [Fact]
    public void TryGetTransformer_WhenAutoTransformationAndInvalidPostfix_ReturnsNull()
    {
        // Arrange
        var transformerManager = new TransformerManager();
        
        // Act
        var result = transformerManager.TryGetTransformer(Transformation.Auto, "invalid.txt");

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void AddTransformer_WhenNameIsJson_ReplacesDefaultJsonTransformer()
    {
        // Arrange
        var transformer = new Mock<ITransformer>();
        var transformation = Transformation.Json;
        var transformerManager = new TransformerManager();
        
        // Act
        var preResult = transformerManager.GetTransformer(transformation);
        transformerManager.AddTransformer(transformation.ToString(), transformer.Object);
        var result = transformerManager.GetTransformer(transformation);

        // Assert
        Assert.NotNull(preResult);
        Assert.IsType<JsonTransformer>(preResult);
        Assert.NotNull(result);
        Assert.Equal(result, transformer.Object);
    }
    
    [Fact]
    public void AddTransformer_WhenNameIsBase64_ReplacesDefaultBase64Transformer()
    {
        // Arrange
        var transformer = new Mock<ITransformer>();
        var transformation = Transformation.Base64;
        var transformerManager = new TransformerManager();
        
        // Act
        var preResult = transformerManager.GetTransformer(transformation);
        transformerManager.AddTransformer(transformation.ToString(), transformer.Object);
        var result = transformerManager.GetTransformer(transformation);

        // Assert
        Assert.NotNull(preResult);
        Assert.IsType<Base64Transformer>(preResult);
        Assert.NotNull(result);
        Assert.Equal(result, transformer.Object);
    }
    
    [Fact]
    public void AddTransformer_WhenNameIsCustom_RegisterCustomTransformer()
    {
        // Arrange
        var transformer = new Mock<ITransformer>();
        var transformation = Guid.NewGuid().ToString();
        var transformerManager = new TransformerManager();
        
        // Act
        var preResult = transformerManager.TryGetTransformer(transformation);
        transformerManager.AddTransformer(transformation, transformer.Object);
        var result = transformerManager.GetTransformer(transformation);

        // Assert
        Assert.Null(preResult);
        Assert.NotNull(result);
        Assert.Equal(result, transformer.Object);
    }
}