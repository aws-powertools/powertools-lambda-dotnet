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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Tracing.Tests.Serializers;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(TestPerson))]
[JsonSerializable(typeof(TestComplexObject))]
[JsonSerializable(typeof(TestResponse))]
[JsonSerializable(typeof(TestBooleanObject))]
[JsonSerializable(typeof(TestNullableObject))]
[JsonSerializable(typeof(TestArrayObject))]
public partial class TestJsonContext : JsonSerializerContext { }

public class TestPerson
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class TestComplexObject
{
    public string StringValue { get; set; }
    public int NumberValue { get; set; }
    public bool BoolValue { get; set; }
    public Dictionary<string, object> NestedObject { get; set; }
}

#endif

public class TestResponse
{
    public string Message { get; set; }
}