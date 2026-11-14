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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using AWS.Cryptography.EncryptionSDK;
using AWS.Cryptography.MaterialProviders;

namespace AWS.Lambda.Powertools.DataMasking.Providers;

/// <summary>
/// Default <see cref="IDataMaskingProvider"/> that performs AWS KMS envelope encryption using the
/// AWS Encryption SDK for .NET.
/// </summary>
/// <remarks>
/// This provider relies on the AWS Encryption SDK for .NET (and its Dafny-based runtime), which uses
/// runtime reflection. It is therefore <b>not</b> compatible with trimming or Native AOT. The type is
/// annotated accordingly.
/// </remarks>
public sealed class AwsEncryptionSdkProvider : IDataMaskingProvider
{
    private readonly ESDK _encryptionSdk;
    private readonly IKeyring _keyring;

    /// <summary>
    /// Creates a provider that protects data with the supplied AWS KMS key(s).
    /// </summary>
    /// <param name="keyIds">
    /// One or more KMS key identifiers. For encryption you can use a key ID, key ARN, alias name, or
    /// alias ARN. For decryption in strict mode a key ARN is required, so prefer key ARNs if the same
    /// keyring is reused for decryption.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="keyIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="keyIds"/> is empty.</exception>
    [RequiresUnreferencedCode("The AWS Encryption SDK for .NET uses runtime reflection and is not compatible with trimming.")]
    [RequiresDynamicCode("The AWS Encryption SDK for .NET uses runtime code generation and is not compatible with Native AOT.")]
    public AwsEncryptionSdkProvider(params string[] keyIds)
        : this(new AmazonKeyManagementServiceClient(), keyIds)
    {
    }

    /// <summary>
    /// Creates a provider with a caller-supplied KMS client (useful for a specific Region or for testing).
    /// </summary>
    /// <param name="kmsClient">The KMS client to use.</param>
    /// <param name="keyIds">One or more KMS key identifiers (see the other constructor for guidance).</param>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="keyIds"/> is empty.</exception>
    [RequiresUnreferencedCode("The AWS Encryption SDK for .NET uses runtime reflection and is not compatible with trimming.")]
    [RequiresDynamicCode("The AWS Encryption SDK for .NET uses runtime code generation and is not compatible with Native AOT.")]
    public AwsEncryptionSdkProvider(IAmazonKeyManagementService kmsClient, params string[] keyIds)
    {
        if (kmsClient is null) throw new ArgumentNullException(nameof(kmsClient));
        if (keyIds is null) throw new ArgumentNullException(nameof(keyIds));
        if (keyIds.Length == 0) throw new ArgumentException("At least one KMS key identifier is required.", nameof(keyIds));

        _encryptionSdk = new ESDK(new AwsEncryptionSdkConfig());
        var materialProviders = new MaterialProviders(new MaterialProvidersConfig());

        // A generator key plus optional additional keys. The first key acts as the generator.
        if (keyIds.Length == 1)
        {
            _keyring = materialProviders.CreateAwsKmsKeyring(new CreateAwsKmsKeyringInput
            {
                KmsClient = kmsClient,
                KmsKeyId = keyIds[0]
            });
        }
        else
        {
            // The generator is the first key; the remaining keys are additional child keys.
            var additionalKeys = new List<string>(keyIds.Length - 1);
            for (var i = 1; i < keyIds.Length; i++)
            {
                additionalKeys.Add(keyIds[i]);
            }

            _keyring = materialProviders.CreateAwsKmsMultiKeyring(new CreateAwsKmsMultiKeyringInput
            {
                Generator = keyIds[0],
                KmsKeyIds = additionalKeys
            });
        }
    }

    /// <inheritdoc />
    public Task<string> EncryptAsync(
        string value,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        cancellationToken.ThrowIfCancellationRequested();

        var encryptInput = new EncryptInput
        {
            Plaintext = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value)),
            Keyring = _keyring
        };

        if (encryptionContext is { Count: > 0 })
        {
            encryptInput.EncryptionContext = new Dictionary<string, string>(encryptionContext);
        }

        var output = _encryptionSdk.Encrypt(encryptInput);
        var ciphertext = ToArray(output.Ciphertext);
        return Task.FromResult(Convert.ToBase64String(ciphertext));
    }

    /// <inheritdoc />
    public Task<string> DecryptAsync(
        string value,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        cancellationToken.ThrowIfCancellationRequested();

        var decryptInput = new DecryptInput
        {
            Ciphertext = new MemoryStream(Convert.FromBase64String(value)),
            Keyring = _keyring
        };

        if (encryptionContext is { Count: > 0 })
        {
            decryptInput.EncryptionContext = new Dictionary<string, string>(encryptionContext);
        }

        var output = _encryptionSdk.Decrypt(decryptInput);
        var plaintext = ToArray(output.Plaintext);
        return Task.FromResult(System.Text.Encoding.UTF8.GetString(plaintext));
    }

    private static byte[] ToArray(MemoryStream stream)
    {
        return stream.ToArray();
    }
}
