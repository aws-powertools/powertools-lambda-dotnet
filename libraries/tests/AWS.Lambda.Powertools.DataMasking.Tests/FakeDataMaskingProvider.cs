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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.DataMasking.Providers;

namespace AWS.Lambda.Powertools.DataMasking.Tests;

/// <summary>
/// A deterministic, in-memory <see cref="IDataMaskingProvider"/> used to exercise the DataMasking
/// Encrypt/Decrypt orchestration without depending on AWS KMS. It base64-encodes with a fixed prefix
/// and, when an encryption context is supplied, binds it so that a mismatched context fails on decrypt.
/// </summary>
internal sealed class FakeDataMaskingProvider : IDataMaskingProvider
{
    private const string Prefix = "enc:";

    public Task<string> EncryptAsync(
        string value,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = SerializeContext(encryptionContext);
        var payload = $"{context}|{value}";
        var encoded = Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        return Task.FromResult(encoded);
    }

    public Task<string> DecryptAsync(
        string value,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default)
    {
        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Value was not produced by this provider.");
        }

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(Prefix.Length)));
        var separator = decoded.IndexOf('|');
        var boundContext = decoded.Substring(0, separator);
        var plaintext = decoded.Substring(separator + 1);

        if (boundContext != SerializeContext(encryptionContext))
        {
            throw new InvalidOperationException("Encryption context mismatch.");
        }

        return Task.FromResult(plaintext);
    }

    private static string SerializeContext(IDictionary<string, string>? context)
    {
        if (context is null || context.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>(context.Count);
        foreach (var kvp in context)
        {
            parts.Add($"{kvp.Key}={kvp.Value}");
        }

        parts.Sort(StringComparer.Ordinal);
        return string.Join(";", parts);
    }
}
