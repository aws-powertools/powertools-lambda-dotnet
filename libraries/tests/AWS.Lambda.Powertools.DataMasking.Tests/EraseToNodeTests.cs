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
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.DataMasking.Tests;

/// <summary>
/// Tests for the logging-oriented convenience that returns a <see cref="JsonNode"/> suitable for
/// passing directly to Powertools Logging (Logger.LogInformation(node)).
/// </summary>
public class EraseToNodeTests
{
    private const string SampleJson = """
    {
      "orderId": "ORD-1001",
      "customer": { "name": "John Doe", "ssn": "123-45-6789" }
    }
    """;

    [Fact]
    public void EraseToNode_ReturnsMaskedNode_WithSensitiveFieldErased()
    {
        var masker = new DataMasking();

        var node = masker.EraseToNode(SampleJson, new[] { "customer.ssn" });

        node.Should().NotBeNull();
        node!["customer"]!["ssn"]!.GetValue<string>().Should().Be("*****");
        node["customer"]!["name"]!.GetValue<string>().Should().Be("John Doe");
        node["orderId"]!.GetValue<string>().Should().Be("ORD-1001");
    }

    [Fact]
    public void EraseToNode_ResultIsSerializable_ForStructuredLogging()
    {
        var masker = new DataMasking();

        var node = masker.EraseToNode(SampleJson, new[] { "customer.ssn" });

        // Simulates what the logger does: serialize the object. Must not throw and must not leak PII.
        var serialized = node!.ToJsonString();
        serialized.Should().NotContain("123-45-6789");
        serialized.Should().Contain("*****");
    }

    [Fact]
    public void EraseToNode_NullJson_Throws()
    {
        var masker = new DataMasking();

        var act = () => masker.EraseToNode(null!, new[] { "customer.ssn" });

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EraseToNode_NullFields_Throws()
    {
        var masker = new DataMasking();

        var act = () => masker.EraseToNode(SampleJson, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
