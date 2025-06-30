/*
 * Copyright JsonCons.Net authors. All Rights Reserved.
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

using System.Runtime.Serialization;
using System.Text;
using AWS.Lambda.Powertools.Kafka.Protobuf;

namespace AWS.Lambda.Powertools.Kafka.Tests;

public class ProtobufErrorHandlingTests
{
    [Fact]
    public void ProtobufSerializer_WithCorruptedKeyData_ThrowSerializationException()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        var corruptedData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        string kafkaEventJson = CreateKafkaEvent(
            Convert.ToBase64String(corruptedData),
            Convert.ToBase64String(Encoding.UTF8.GetBytes("valid-value"))
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act & Assert
        var ex = Assert.Throws<SerializationException>(() =>
            serializer.Deserialize<ConsumerRecords<TestModel, string>>(stream));

        Assert.Contains("Failed to deserialize key data", ex.Message);
    }

    [Fact]
    public void ProtobufSerializer_WithCorruptedValueData_ThrowSerializationException()
    {
        // Arrange
        var serializer = new PowertoolsKafkaProtobufSerializer();
        var corruptedData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        string kafkaEventJson = CreateKafkaEvent(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("valid-key")),
            Convert.ToBase64String(corruptedData)
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kafkaEventJson));

        // Act & Assert
        var ex = Assert.Throws<SerializationException>(() =>
            serializer.Deserialize<ConsumerRecords<string, TestModel>>(stream));

        Assert.Contains("Failed to deserialize value data", ex.Message);
    }

    private string CreateKafkaEvent(string keyValue, string valueValue)
    {
        return @$"{{
            ""eventSource"": ""aws:kafka"",
            ""records"": {{
                ""mytopic-0"": [
                    {{
                        ""topic"": ""mytopic"",
                        ""partition"": 0,
                        ""offset"": 15,
                        ""key"": ""{keyValue}"",
                        ""value"": ""{valueValue}""
                    }}
                ]
            }}
        }}";
    }
}