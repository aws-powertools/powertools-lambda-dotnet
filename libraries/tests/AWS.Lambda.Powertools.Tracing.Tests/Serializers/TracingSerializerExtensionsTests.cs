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
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Tracing.Serializers;
using Xunit;

namespace AWS.Lambda.Powertools.Tracing.Tests.Serializers;

public class TracingSerializerExtensionsTests
{
    [Fact]
    public void WithTracing_InitializesSerializer_Successfully()
    {
        // Arrange
        var serializer = new SourceGeneratorLambdaJsonSerializer<TestJsonContext>();

        // Act
        var result = serializer.WithTracing();

        // Assert
        Assert.NotNull(result);
        
        // Verify the context was initialized by attempting to serialize
        var testObject = new TestPerson { Name = "Test", Age = 25 };
        var serialized = PowertoolsTracingSerializer.Serialize(testObject);
        Assert.Contains("\"Name\":\"Test\"", serialized);
    }
}
#endif