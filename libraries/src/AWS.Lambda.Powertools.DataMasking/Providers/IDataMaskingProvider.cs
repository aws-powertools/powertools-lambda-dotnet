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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.DataMasking.Providers;

/// <summary>
/// Abstraction for the encryption backend used by <see cref="DataMasking"/> to encrypt and decrypt values.
/// </summary>
/// <remarks>
/// The default implementation, <see cref="AwsEncryptionSdkProvider"/>, uses the AWS Encryption SDK for .NET
/// with AWS KMS envelope encryption. Implement this interface to provide a custom encryption backend.
/// </remarks>
public interface IDataMaskingProvider
{
    /// <summary>
    /// Encrypts a plaintext value.
    /// </summary>
    /// <param name="value">The plaintext value to encrypt.</param>
    /// <param name="encryptionContext">
    /// Optional non-secret key-value pairs bound to the ciphertext as additional authenticated data.
    /// The same context must be supplied (and is validated) on decrypt.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The encrypted value, encoded as a string that can later be passed to <see cref="DecryptAsync"/>.</returns>
    Task<string> EncryptAsync(
        string value,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts a value previously produced by <see cref="EncryptAsync"/>.
    /// </summary>
    /// <param name="value">The encrypted value.</param>
    /// <param name="encryptionContext">
    /// Optional encryption context to validate against the context bound at encrypt time.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The decrypted plaintext value.</returns>
    Task<string> DecryptAsync(
        string value,
        IDictionary<string, string>? encryptionContext = null,
        CancellationToken cancellationToken = default);
}
