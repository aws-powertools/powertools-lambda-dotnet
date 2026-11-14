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
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.DataMasking.Tests;

public class EraseTests
{
    private const string SampleJson = """
    {
      "email": "john@example.com",
      "address": { "street": "123 Main St", "city": "Anytown" },
      "customer": { "ssn": "123-45-6789" }
    }
    """;

    [Fact]
    public void Erase_TopLevelField_ReplacesWithDefaultMask()
    {
        var masker = new DataMasking();

        var result = masker.Erase(SampleJson, new[] { "email" });

        var node = JsonNode.Parse(result)!;
        node["email"]!.GetValue<string>().Should().Be("*****");
    }

    [Fact]
    public void Erase_NestedField_MasksOnlyThatField_AndKeepsSiblings()
    {
        var masker = new DataMasking();

        var result = masker.Erase(SampleJson, new[] { "address.street" });

        var node = JsonNode.Parse(result)!;
        node["address"]!["street"]!.GetValue<string>().Should().Be("*****");
        // Sibling remains readable
        node["address"]!["city"]!.GetValue<string>().Should().Be("Anytown");
    }

    [Fact]
    public void Erase_MultipleFields_MasksAllOfThem()
    {
        var masker = new DataMasking();

        var result = masker.Erase(SampleJson, new[] { "email", "address.street", "customer.ssn" });

        var node = JsonNode.Parse(result)!;
        node["email"]!.GetValue<string>().Should().Be("*****");
        node["address"]!["street"]!.GetValue<string>().Should().Be("*****");
        node["customer"]!["ssn"]!.GetValue<string>().Should().Be("*****");
        node["address"]!["city"]!.GetValue<string>().Should().Be("Anytown");
    }

    [Fact]
    public void Erase_MissingField_IsSkipped_WithoutError()
    {
        var masker = new DataMasking();

        var result = masker.Erase(SampleJson, new[] { "does.not.exist", "email" });

        var node = JsonNode.Parse(result)!;
        // Existing field still masked, missing path silently ignored
        node["email"]!.GetValue<string>().Should().Be("*****");
        node["address"]!["city"]!.GetValue<string>().Should().Be("Anytown");
    }

    [Fact]
    public void Erase_PathThroughNonObject_IsSkipped()
    {
        var masker = new DataMasking();

        // "email" is a string, so "email.something" cannot be traversed
        var result = masker.Erase(SampleJson, new[] { "email.something" });

        var node = JsonNode.Parse(result)!;
        node["email"]!.GetValue<string>().Should().Be("john@example.com");
    }

    [Fact]
    public void Erase_PreserveLength_UsesMaskCharacterMatchingOriginalLength()
    {
        var masker = new DataMasking();
        var options = new MaskingOptions { PreserveLength = true };

        var result = masker.Erase(SampleJson, new[] { "customer.ssn" }, options);

        var node = JsonNode.Parse(result)!;
        // "123-45-6789" has 11 characters
        node["customer"]!["ssn"]!.GetValue<string>().Should().Be(new string('*', "123-45-6789".Length));
    }

    [Fact]
    public void Erase_CustomMaskValue_IsUsed()
    {
        var masker = new DataMasking();
        var options = new MaskingOptions { MaskValue = "[REDACTED]" };

        var result = masker.Erase(SampleJson, new[] { "email" }, options);

        var node = JsonNode.Parse(result)!;
        node["email"]!.GetValue<string>().Should().Be("[REDACTED]");
    }

    [Fact]
    public void Erase_WithRegexPattern_AppliesSubstitution()
    {
        var masker = new DataMasking();
        var options = new MaskingOptions
        {
            Pattern = new Regex(@"^(.).*@"),
            Replacement = "$1****@"
        };

        var result = masker.Erase(SampleJson, new[] { "email" }, options);

        var node = JsonNode.Parse(result)!;
        node["email"]!.GetValue<string>().Should().Be("j****@example.com");
    }

    [Fact]
    public void Erase_JsonNodeOverload_MasksInPlace_AndReturnsSameInstance()
    {
        var masker = new DataMasking();
        var node = JsonNode.Parse(SampleJson)!;

        var returned = masker.Erase(node, new[] { "email" });

        returned.Should().BeSameAs(node);
        node["email"]!.GetValue<string>().Should().Be("*****");
    }

    [Fact]
    public void Erase_ObjectOverload_SerializesAndMasks()
    {
        var masker = new DataMasking();
        var data = new { email = "john@example.com", name = "John" };

#pragma warning disable IL2026, IL3050 // Reflection-based overload exercised intentionally in tests
        var result = masker.Erase(data, new[] { "email" });
#pragma warning restore IL2026, IL3050

        var node = JsonNode.Parse(result)!;
        node["email"]!.GetValue<string>().Should().Be("*****");
        node["name"]!.GetValue<string>().Should().Be("John");
    }

    [Fact]
    public void Erase_NullJson_Throws()
    {
        var masker = new DataMasking();

        var act = () => masker.Erase((string)null!, new[] { "email" });

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Erase_NullFields_Throws()
    {
        var masker = new DataMasking();

        var act = () => masker.Erase(SampleJson, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Erase_EmptyOrWhitespaceFieldPaths_AreIgnored()
    {
        var masker = new DataMasking();

        var result = masker.Erase(SampleJson, new[] { "", "   ", "email" });

        var node = JsonNode.Parse(result)!;
        node["email"]!.GetValue<string>().Should().Be("*****");
    }
}
