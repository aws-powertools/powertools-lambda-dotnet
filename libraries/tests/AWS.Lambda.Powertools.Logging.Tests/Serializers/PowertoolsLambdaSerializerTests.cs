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
using System.Text.Json.Serialization.Metadata;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Tests.Utilities;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Serializers;

public class PowertoolsLambdaSerializerTests : IDisposable
{
    [Fact]
    public void Constructor_ShouldNotThrowException()
    {
        // Arrange & Act & Assert
        var exception =
            Record.Exception(() => PowertoolsLoggingSerializer.AddSerializerContext(TestJsonContext.Default));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_ShouldAddCustomerContext()
    {
        // Arrange
        var customerContext = new TestJsonContext();

        // Act
        PowertoolsLoggingSerializer.AddSerializerContext(customerContext);
        ;

        // Assert
        Assert.True(PowertoolsLoggingSerializer.HasContext(customerContext));
    }

    [Theory]
    [InlineData(LoggerOutputCase.CamelCase, "{\"fullName\":\"John\",\"age\":30}", "John", 30)]
    [InlineData(LoggerOutputCase.PascalCase, "{\"FullName\":\"Jane\",\"Age\":25}", "Jane", 25)]
    public void Deserialize_ValidJson_ShouldReturnDeserializedObject(LoggerOutputCase outputCase, string json,
        string expectedName, int expectedAge)
    {
        // Arrange
        var serializer = new PowertoolsSourceGeneratorSerializer<TestJsonContext>();

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
        var serializer = new PowertoolsSourceGeneratorSerializer<TestJsonContext>();
        ;

        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggerOutputCase.PascalCase);

        var json = "{\"FullName\":\"John\",\"Age\":30}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act & Assert
        Assert.Throws<JsonSerializerException>(() => serializer.Deserialize<UnknownType>(stream));
    }

    [Fact]
    public void Serialize_ValidObject_ShouldSerializeToStream()
    {
        // Arrange
        var serializer = new PowertoolsSourceGeneratorSerializer<TestJsonContext>();
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
        var serializer = new PowertoolsSourceGeneratorSerializer<TestJsonContext>();
        ;
        var unknownObject = new UnknownType();
        var stream = new MemoryStream();

        // Act & Assert
        Assert.Throws<JsonSerializerException>(() => serializer.Serialize(unknownObject, stream));
    }

    private class UnknownType
    {
    }

    [Fact]
    public void Deserialize_NonSeekableStream_ShouldDeserializeCorrectly()
    {
        // Arrange
        var serializer = new PowertoolsSourceGeneratorSerializer<TestJsonContext>();
        ;
        var json = "{\"fullName\":\"John\",\"age\":30}";
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
        public override void Close()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }


    [Fact]
    public void Should_Serialize_Unknown_Type_When_Including_Outside_Context()
    {
        // Arrange
        var serializer = new PowertoolsSourceGeneratorSerializer<TestJsonContext>();
        var testObject = new APIGatewayProxyRequest
        {
            Path = "asda",
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                RequestId = "asdas"
            }
        };

        var log = new LogEntry
        {
            Name = "dasda",
            Message = testObject
        };

        var stream = new MemoryStream();

        // Act
        serializer.Serialize(testObject, stream);

        stream.Position = 0;
        var outputExternalSerializer = new StreamReader(stream).ReadToEnd();

        var outptuMySerializer = PowertoolsLoggingSerializer.Serialize(log, typeof(LogEntry));

        // Assert
        Assert.Equal(
            "{\"Path\":\"asda\",\"RequestContext\":{\"RequestId\":\"asdas\",\"ConnectedAt\":0,\"RequestTimeEpoch\":0},\"IsBase64Encoded\":false}",
            outputExternalSerializer);
        Assert.Equal(
            "{\"cold_start\":false,\"x_ray_trace_id\":null,\"correlation_id\":null,\"timestamp\":\"0001-01-01T00:00:00\",\"level\":\"Trace\",\"service\":null,\"name\":\"dasda\",\"message\":{\"resource\":null,\"path\":\"asda\",\"http_method\":null,\"headers\":null,\"multi_value_headers\":null,\"query_string_parameters\":null,\"multi_value_query_string_parameters\":null,\"path_parameters\":null,\"stage_variables\":null,\"request_context\":{\"path\":null,\"account_id\":null,\"resource_id\":null,\"stage\":null,\"request_id\":\"asdas\",\"identity\":null,\"resource_path\":null,\"http_method\":null,\"api_id\":null,\"extended_request_id\":null,\"connection_id\":null,\"connected_at\":0,\"domain_name\":null,\"domain_prefix\":null,\"event_type\":null,\"message_id\":null,\"route_key\":null,\"authorizer\":null,\"operation_name\":null,\"error\":null,\"integration_latency\":null,\"message_direction\":null,\"request_time\":null,\"request_time_epoch\":0,\"status\":null},\"body\":null,\"is_base64_encoded\":false},\"sampling_rate\":null,\"extra_keys\":null,\"exception\":null,\"lambda_context\":null}",
            outptuMySerializer);
    }

    public void Dispose()
    {
        PowertoolsLoggingSerializer.ConfigureNamingPolicy(LoggingConstants.DefaultLoggerOutputCase);
        PowertoolsLoggingSerializer.ClearOptions();
    }
}
#endif