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

using AWS.Lambda.Powertools.Idempotency.Output;

namespace AWS.Lambda.Powertools.Idempotency;

/// <summary>
/// Configuration of the idempotency feature. Use the Builder to create an instance.
/// </summary>
public class IdempotencyOptions
{
    /// <summary>
    /// A JMESPath expression to extract the idempotency key from the event record.
    /// <seealso href="https://jmespath.org">https://jmespath.org</seealso> for more details
    /// Common paths:
    /// <c>powertools_json(Body)</c> for APIGatewayProxyRequest
    /// <c>Records[*].powertools_json(Body)</c> for SQSEvent
    /// <c>Records[0].Sns.Message | powertools_json(@)</c> for SNSEvent
    /// <c>Detail</c> for ScheduledEvent (EventBridge / CloudWatch events)
    /// </summary>
    public string EventKeyJmesPath { get; }
    /// <summary>
    /// JMES Path of a part of the payload to be used for validation
    /// See <seealso href="https://jmespath.org/">https://jmespath.org/</seealso>
    /// </summary>
    public string PayloadValidationJmesPath { get; }
    /// <summary>
    /// Boolean to indicate if we must throw an Exception when
    /// idempotency key could not be found in the payload.
    /// </summary>
    public bool ThrowOnNoIdempotencyKey { get; }
    /// <summary>
    /// Whether to locally cache idempotency results, by default false
    /// </summary>
    public bool UseLocalCache { get; }
    /// <summary>
    /// The maximum number of items to store in local cache
    /// </summary>
    public int LocalCacheMaxItems { get; }
    /// <summary>
    /// The number of seconds to wait before a record is expired
    /// </summary>
    public long ExpirationInSeconds { get; }
    /// <summary>
    /// Algorithm to use for calculating hashes,
    /// as supported by <see cref="System.Security.Cryptography.HashAlgorithm"/> (eg. SHA1, SHA-256, ...)
    /// </summary>
    public string HashFunction { get; }
    
    /// <summary>
    /// Instance of ILog to record internal details of idempotency 
    /// </summary>
    public ILog Log { get; }

    internal IdempotencyOptions(
        string eventKeyJmesPath, 
        string payloadValidationJmesPath, 
        bool throwOnNoIdempotencyKey, 
        bool useLocalCache, 
        int localCacheMaxItems, 
        long expirationInSeconds, 
        string hashFunction,
        ILog log)
    {
        EventKeyJmesPath = eventKeyJmesPath;
        PayloadValidationJmesPath = payloadValidationJmesPath;
        ThrowOnNoIdempotencyKey = throwOnNoIdempotencyKey;
        UseLocalCache = useLocalCache;
        LocalCacheMaxItems = localCacheMaxItems;
        ExpirationInSeconds = expirationInSeconds;
        HashFunction = hashFunction;
        Log = log;
    }
}