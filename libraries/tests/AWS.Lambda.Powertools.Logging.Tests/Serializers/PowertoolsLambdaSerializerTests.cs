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

using AWS.Lambda.Powertools.Logging.Serializers;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Logging.Internal;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Serializers;

[JsonSerializable(typeof(TestObject))]
public partial class TestJsonContext : JsonSerializerContext
{
}

public class TestObject
{
    public string FullName { get; set; }
    public int Age { get; set; }
}

public class PowertoolsLambdaSerializerTests : IDisposable
{
    [Fact]
    public void Constructor_ShouldNotThrowException()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => new PowertoolsLambdaSerializer(TestJsonContext.Default));
        Assert.Null(exception);
    }
    
    [Fact]
    public void Constructor_ShouldAddCustomerContext()
    {
        // Arrange
        var customerContext = new TestJsonContext();
    
        // Act
        var serializer = new PowertoolsLambdaSerializer(customerContext);
    
        // Assert
        Assert.True(PowertoolsLoggingSerializer.HasContext(customerContext));
    }

    [Theory]
    [InlineData(LoggerOutputCase.CamelCase,"{\"fullName\":\"John\",\"age\":30}", "John", 30)]
    [InlineData(LoggerOutputCase.PascalCase,"{\"FullName\":\"Jane\",\"Age\":25}", "Jane", 25)]
    [InlineData(LoggerOutputCase.SnakeCase,"{\"full_name\":\"Jane\",\"age\":25}", "Jane", 25)]
    public void Deserialize_ValidJson_ShouldReturnDeserializedObject(LoggerOutputCase outputCase,string json, string expectedName, int expectedAge)
    {
        // Arrange
        var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(outputCase);
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = serializer.Deserialize<TestObject>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedName, result.FullName);
        Assert.Equal(expectedAge, result.Age);
    }

    [Fact]
    public void Deserialize_InvalidType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
        
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase.PascalCase);
        
        var json = "{\"FullName\":\"John\",\"Age\":30}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize<UnknownType>(stream));
    }

    [Fact]
    public void Serialize_ValidObject_ShouldSerializeToStream()
    {
        // Arrange
        var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
        
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase.PascalCase);
        
        var testObject = new TestObject { FullName = "Jane", Age = 25 };
        var stream = new MemoryStream();

        // Act
        serializer.Serialize(testObject, stream);

        // Assert
        stream.Position = 0;
        var result = new StreamReader(stream).ReadToEnd();
        Assert.Contains("\"FullName\":\"Jane\"", result);
        Assert.Contains("\"Age\":25", result);
    }

    [Fact]
    public void Serialize_InvalidType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
        var unknownObject = new UnknownType();
        var stream = new MemoryStream();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => serializer.Serialize(unknownObject, stream));
    }

    private class UnknownType { }
    
    [Fact]
    public void Deserialize_NonSeekableStream_ShouldDeserializeCorrectly()
    {
        // Arrange
        var serializer = new PowertoolsLambdaSerializer(TestJsonContext.Default);
        var json = "{\"full_name\":\"John\",\"age\":30}";
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var nonSeekableStream = new NonSeekableStream(jsonBytes);

        // Act
        var result = serializer.Deserialize<TestObject>(nonSeekableStream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FullName);
        Assert.Equal(30, result.Age);
    }
    
    public class NonSeekableStream : Stream
    {
        private readonly MemoryStream _innerStream;

        public NonSeekableStream(byte[] data)
        {
            _innerStream = new MemoryStream(data);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => _innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        // Override the Close and Dispose methods to prevent the inner stream from being closed
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
    }

    public void Dispose()
    {
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggingConstants.DefaultLoggerOutputCase);
    }
}
#endif

