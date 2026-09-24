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
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.DataMasking.Tests;

public class EncryptDecryptTests
{
    private const string SampleJson = """
    {
      "orderId": "ORD-1001",
      "customer": { "name": "John Doe", "ssn": "123-45-6789" },
      "payment": { "creditCard": "4111111111111111", "amount": 42.5 }
    }
    """;

    private static DataMasking CreateMasker() => new(new FakeDataMaskingProvider());

    [Fact]
    public async Task EncryptDecrypt_FullPayload_RoundTrips()
    {
        var masker = CreateMasker();

        var encrypted = await masker.EncryptAsync(SampleJson);
        encrypted.Should().NotContain("123-45-6789");

        var decrypted = await masker.DecryptAsync(encrypted);

        // Compare structurally (formatting/whitespace independent)
        var expected = JsonNode.Parse(SampleJson)!.ToJsonString();
        var actual = JsonNode.Parse(decrypted)!.ToJsonString();
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task EncryptDecrypt_FieldLevel_LeavesOtherFieldsReadable()
    {
        var masker = CreateMasker();
        var fields = new[] { "customer.ssn", "payment.creditCard" };

        var encrypted = await masker.EncryptAsync(SampleJson, fields);

        var node = JsonNode.Parse(encrypted)!;
        // Selected fields are encrypted...
        node["customer"]!["ssn"]!.GetValue<string>().Should().StartWith("enc:");
        node["payment"]!["creditCard"]!.GetValue<string>().Should().StartWith("enc:");
        // ...the rest stays readable
        node["orderId"]!.GetValue<string>().Should().Be("ORD-1001");
        node["customer"]!["name"]!.GetValue<string>().Should().Be("John Doe");

        var decrypted = await masker.DecryptAsync(encrypted, fields);
        var restored = JsonNode.Parse(decrypted)!;
        restored["customer"]!["ssn"]!.GetValue<string>().Should().Be("123-45-6789");
        restored["payment"]!["creditCard"]!.GetValue<string>().Should().Be("4111111111111111");
    }

    [Fact]
    public async Task EncryptDecrypt_WithMatchingEncryptionContext_RoundTrips()
    {
        var masker = CreateMasker();
        var context = new Dictionary<string, string> { ["tenantId"] = "acme-corp" };

        var encrypted = await masker.EncryptAsync(SampleJson, context);
        var decrypted = await masker.DecryptAsync(encrypted, context);

        JsonNode.Parse(decrypted)!.ToJsonString()
            .Should().Be(JsonNode.Parse(SampleJson)!.ToJsonString());
    }

    [Fact]
    public async Task Decrypt_WithMismatchedEncryptionContext_Throws()
    {
        var masker = CreateMasker();
        var encryptContext = new Dictionary<string, string> { ["tenantId"] = "acme-corp" };
        var wrongContext = new Dictionary<string, string> { ["tenantId"] = "evil-corp" };

        var encrypted = await masker.EncryptAsync(SampleJson, encryptContext);

        var act = async () => await masker.DecryptAsync(encrypted, wrongContext);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Encrypt_WithoutProvider_Throws()
    {
        var masker = new DataMasking(); // no provider

        var act = async () => await masker.EncryptAsync(SampleJson);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Decrypt_WithoutProvider_Throws()
    {
        var masker = new DataMasking(); // no provider

        var act = async () => await masker.DecryptAsync("enc:whatever");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_NullProvider_Throws()
    {
        var act = () => new DataMasking(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task EncryptAsync_NullJson_Throws()
    {
        var masker = CreateMasker();

        var act = async () => await masker.EncryptAsync((string)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
