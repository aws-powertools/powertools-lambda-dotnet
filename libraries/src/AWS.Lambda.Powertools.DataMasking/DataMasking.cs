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
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.DataMasking.Internal;
using AWS.Lambda.Powertools.DataMasking.Providers;

namespace AWS.Lambda.Powertools.DataMasking;

/// <summary>
/// Provides masking of sensitive fields within a payload: irreversible <c>Erase</c>, and reversible
/// <c>Encrypt</c>/<c>Decrypt</c> when constructed with an <see cref="IDataMaskingProvider"/>.
/// </summary>
/// <remarks>
/// Field selection uses simple dotted paths (for example <c>"address.street"</c>). Full JMESPath
/// expression support is planned for a later phase.
/// </remarks>
public sealed class DataMasking
{
    private readonly IDataMaskingProvider? _provider;

    /// <summary>
    /// Creates an instance that supports the <c>Erase</c> operations only.
    /// Use <see cref="DataMasking(IDataMaskingProvider)"/> to enable <c>Encrypt</c>/<c>Decrypt</c>.
    /// </summary>
    public DataMasking()
    {
    }

    /// <summary>
    /// Creates an instance backed by an encryption provider, enabling <c>Encrypt</c>/<c>Decrypt</c>
    /// in addition to <c>Erase</c>.
    /// </summary>
    /// <param name="provider">The encryption backend (for example <c>AwsEncryptionSdkProvider</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="provider"/> is <see langword="null"/>.</exception>
    public DataMasking(IDataMaskingProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// Irreversibly masks the supplied fields within a JSON string and returns the result as a
    /// <see cref="JsonNode"/>, ready to be passed to structured logging (for example
    /// <c>Logger.LogInformation(masker.EraseToNode(json, fields))</c>) without an extra string round-trip.
    /// This overload is fully trimming/AOT safe.
    /// </summary>
    /// <param name="json">The JSON payload as a string.</param>
    /// <param name="fields">The dotted field paths to mask.</param>
    /// <param name="options">Optional masking options. Defaults to a fixed <c>*****</c> mask.</param>
    /// <returns>The masked payload as a <see cref="JsonNode"/> (or <see langword="null"/> if the payload was JSON null).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> or <paramref name="fields"/> is <see langword="null"/>.</exception>
    public JsonNode? EraseToNode(string json, string[] fields, MaskingOptions? options = null)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        return MaskRoot(JsonNode.Parse(json), fields, options);
    }

    /// <summary>
    /// Irreversibly masks the supplied fields within a JSON string.
    /// This overload is fully trimming/AOT safe.
    /// </summary>
    /// <param name="json">The JSON payload as a string.</param>
    /// <param name="fields">The dotted field paths to mask (for example <c>"customer.ssn"</c>).</param>
    /// <param name="options">Optional masking options. Defaults to a fixed <c>*****</c> mask.</param>
    /// <returns>The masked payload serialized back to a JSON string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> or <paramref name="fields"/> is <see langword="null"/>.</exception>
    public string Erase(string json, string[] fields, MaskingOptions? options = null)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        return Serialize(MaskRoot(JsonNode.Parse(json), fields, options));
    }

    /// <summary>
    /// Irreversibly masks the supplied fields within a <see cref="JsonNode"/>, mutating it in place.
    /// This overload is fully trimming/AOT safe.
    /// </summary>
    /// <param name="node">The payload as a <see cref="JsonNode"/>. Masked in place.</param>
    /// <param name="fields">The dotted field paths to mask.</param>
    /// <param name="options">Optional masking options. Defaults to a fixed <c>*****</c> mask.</param>
    /// <returns>The same <paramref name="node"/> instance, masked in place.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> or <paramref name="fields"/> is <see langword="null"/>.</exception>
    public JsonNode Erase(JsonNode node, string[] fields, MaskingOptions? options = null)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        JsonNodeMasker.MaskFields(node, fields, options ?? MaskingOptions.Default);
        return node;
    }

    /// <summary>
    /// Irreversibly masks the supplied fields within an object, returning the masked payload as a JSON string.
    /// </summary>
    /// <remarks>
    /// This overload serializes <paramref name="data"/> using reflection-based
    /// <see cref="System.Text.Json"/> and is therefore not compatible with trimming/Native AOT.
    /// For AOT scenarios use <see cref="Erase(string,string[],MaskingOptions)"/>,
    /// <see cref="Erase(JsonNode,string[],MaskingOptions)"/>, or
    /// <see cref="Erase(object,JsonTypeInfo,string[],MaskingOptions)"/> with a source-generated context.
    /// </remarks>
    /// <param name="data">The object to mask.</param>
    /// <param name="fields">The dotted field paths to mask.</param>
    /// <param name="options">Optional masking options. Defaults to a fixed <c>*****</c> mask.</param>
    /// <returns>The masked payload serialized to a JSON string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> or <paramref name="fields"/> is <see langword="null"/>.</exception>
    [RequiresUnreferencedCode("JSON serialization of the supplied object might require types that cannot be statically analyzed. Use an overload that accepts a JsonTypeInfo for trimming/AOT scenarios.")]
    [RequiresDynamicCode("JSON serialization of the supplied object might require runtime code generation. Use an overload that accepts a JsonTypeInfo for trimming/AOT scenarios.")]
    public string Erase(object data, string[] fields, MaskingOptions? options = null)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        return Serialize(MaskRoot(JsonSerializer.SerializeToNode(data), fields, options));
    }

    /// <summary>
    /// Irreversibly masks the supplied fields within an object using a source-generated
    /// <see cref="JsonTypeInfo"/>, returning the masked payload as a JSON string.
    /// This overload is trimming/AOT safe when supplied with a source-generated type info.
    /// </summary>
    /// <param name="data">The object to mask.</param>
    /// <param name="jsonTypeInfo">The source-generated type metadata used to serialize <paramref name="data"/>.</param>
    /// <param name="fields">The dotted field paths to mask.</param>
    /// <param name="options">Optional masking options. Defaults to a fixed <c>*****</c> mask.</param>
    /// <returns>The masked payload serialized to a JSON string.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    public string Erase(object data, JsonTypeInfo jsonTypeInfo, string[] fields, MaskingOptions? options = null)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (jsonTypeInfo is null) throw new ArgumentNullException(nameof(jsonTypeInfo));
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        return Serialize(MaskRoot(JsonSerializer.SerializeToNode(data, jsonTypeInfo), fields, options));
    }

    /// <summary>
    /// Encrypts the entire JSON payload using the configured <see cref="IDataMaskingProvider"/>.
    /// </summary>
    /// <param name="json">The JSON payload as a string.</param>
    /// <param name="encryptionContext">Optional encryption context bound to the ciphertext.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The encrypted payload.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No provider was configured.</exception>
    public Task<string> EncryptAsync(
        string json,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));
        return RequireProvider().EncryptAsync(json, encryptionContext, cancellationToken);
    }

    /// <summary>
    /// Encrypts only the supplied fields within a JSON payload, leaving the rest readable.
    /// </summary>
    /// <param name="json">The JSON payload as a string.</param>
    /// <param name="fields">The dotted field paths to encrypt.</param>
    /// <param name="encryptionContext">Optional encryption context bound to each field's ciphertext.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The payload with the selected fields encrypted, serialized to a JSON string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> or <paramref name="fields"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No provider was configured.</exception>
    public async Task<string> EncryptAsync(
        string json,
        string[] fields,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        var provider = RequireProvider();
        return await TransformFieldsToStringAsync(json, fields,
            value => provider.EncryptAsync(value, encryptionContext, cancellationToken));
    }

    /// <summary>
    /// Decrypts an entire payload previously produced by <see cref="EncryptAsync(string,IDictionary{string,string},CancellationToken)"/>.
    /// </summary>
    /// <param name="value">The encrypted payload.</param>
    /// <param name="encryptionContext">Optional encryption context to validate against the value bound at encrypt time.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The decrypted payload.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No provider was configured.</exception>
    public Task<string> DecryptAsync(
        string value,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return RequireProvider().DecryptAsync(value, encryptionContext, cancellationToken);
    }

    /// <summary>
    /// Decrypts only the supplied fields within a JSON payload previously produced by
    /// <see cref="EncryptAsync(string,string[],IDictionary{string,string},CancellationToken)"/>.
    /// </summary>
    /// <param name="json">The JSON payload as a string.</param>
    /// <param name="fields">The dotted field paths to decrypt.</param>
    /// <param name="encryptionContext">Optional encryption context to validate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The payload with the selected fields decrypted, serialized to a JSON string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> or <paramref name="fields"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No provider was configured.</exception>
    public async Task<string> DecryptAsync(
        string json,
        string[] fields,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        var provider = RequireProvider();
        return await TransformFieldsToStringAsync(json, fields,
            value => provider.DecryptAsync(value, encryptionContext, cancellationToken));
    }

    private IDataMaskingProvider RequireProvider()
    {
        return _provider ?? throw new InvalidOperationException(
            "This operation requires an encryption provider. Construct DataMasking with an IDataMaskingProvider (for example new DataMasking(new AwsEncryptionSdkProvider(keyArn))).");
    }

    private static JsonNode? MaskRoot(JsonNode? root, string[] fields, MaskingOptions? options)
    {
        JsonNodeMasker.MaskFields(root, fields, options ?? MaskingOptions.Default);
        return root;
    }

    private static string Serialize(JsonNode? root) => root?.ToJsonString() ?? "null";

    private static async Task<string> TransformFieldsToStringAsync(
        string json, string[] fields, Func<string, Task<string>> transform)
    {
        var root = JsonNode.Parse(json);
        await JsonNodeMasker.TransformFieldsAsync(root, fields, transform);
        return Serialize(root);
    }
}
