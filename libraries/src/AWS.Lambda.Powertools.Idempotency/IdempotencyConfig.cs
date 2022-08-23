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
using AWS.Lambda.Powertools.Idempotency.Output;

namespace AWS.Lambda.Powertools.Idempotency;

/// <summary>
/// Configuration of the idempotency feature. Use the Builder to create an instance.
/// </summary>
public class IdempotencyConfig
{
    public string EventKeyJmesPath { get; }
    public string PayloadValidationJmesPath { get; }
    public bool ThrowOnNoIdempotencyKey { get; }
    public bool UseLocalCache { get; }
    public int LocalCacheMaxItems { get; }
    public long ExpirationInSeconds { get; }
    public string HashFunction { get; }
    public ILog Log { get; }

    private IdempotencyConfig(
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

    public static IdempotencyConfigBuilder Builder()
    {
        return new IdempotencyConfigBuilder();
    }
    
    public class IdempotencyConfigBuilder
    {
        private int _localCacheMaxItems = 256;
        private bool _useLocalCache = false;
        private long _expirationInSeconds = 60 * 60; // 1 hour
        private string _eventKeyJmesPath = null;
        private string _payloadValidationJmesPath;
        private bool _throwOnNoIdempotencyKey = false;
        private string _hashFunction = "MD5";
        private ILog _log = new ConsoleLog();
        
        /// <summary>
        /// Initialize and return an instance of IdempotencyConfig.
        /// Example:
        /// IdempotencyConfig.Builder().WithUseLocalCache().Build();
        /// This instance must then be passed to the Idempotency.Config:
        /// Idempotency.Config().WithConfig(config).Configure();
        /// </summary>
        /// <returns>an instance of IdempotencyConfig</returns>
        public IdempotencyConfig Build() =>
            new IdempotencyConfig(_eventKeyJmesPath, 
                _payloadValidationJmesPath, 
                _throwOnNoIdempotencyKey,
                _useLocalCache, 
                _localCacheMaxItems, 
                _expirationInSeconds, 
                _hashFunction,
                _log);

        /// <summary>
        /// A JMESPath expression to extract the idempotency key from the event record.
        /// See <a href="https://jmespath.org/">https://jmespath.org/</a> for more details.
        /// </summary>
        /// <param name="eventKeyJmesPath">path of the key in the Lambda event</param>
        /// <returns>the instance of the builder (to chain operations)</returns>
        public IdempotencyConfigBuilder WithEventKeyJmesPath(string eventKeyJmesPath)
        {
            _eventKeyJmesPath = eventKeyJmesPath;
            return this;
        }

        /// <summary>
        /// Whether to locally cache idempotency results, by default false
        /// </summary>
        /// <param name="useLocalCache">Indicate if a local cache must be used in addition to the persistence store.</param>
        /// <returns>the instance of the builder (to chain operations)</returns>
        public IdempotencyConfigBuilder WithUseLocalCache(bool useLocalCache)
        {
            _useLocalCache = useLocalCache;
            return this;
        }
        
        /// <summary>
        /// A JMESPath expression to extract the payload to be validated from the event record.
        /// See <a href="https://jmespath.org/">https://jmespath.org/</a> for more details.
        /// </summary>
        /// <param name="payloadValidationJmesPath">JMES Path of a part of the payload to be used for validation</param>
        /// <returns>the instance of the builder (to chain operations)</returns>
        public IdempotencyConfigBuilder WithPayloadValidationJmesPath(string payloadValidationJmesPath)
        {
            _payloadValidationJmesPath = payloadValidationJmesPath;
            return this;
        }
        
        /// <summary>
        /// Whether to throw an exception if no idempotency key was found in the request, by default false
        /// </summary>
        /// <param name="throwOnNoIdempotencyKey">boolean to indicate if we must throw an Exception when
        ///                                idempotency key could not be found in the payload.</param>
        /// <returns>the instance of the builder (to chain operations)</returns>
        public IdempotencyConfigBuilder WithThrowOnNoIdempotencyKey(bool throwOnNoIdempotencyKey)
        {
            _throwOnNoIdempotencyKey = throwOnNoIdempotencyKey;
            return this;
        }
        
        /// <summary>
        /// The number of seconds to wait before a record is expired
        /// </summary>
        /// <param name="duration">expiration of the record in the store</param>
        /// <returns>the instance of the builder (to chain operations)</returns>
        public IdempotencyConfigBuilder WithExpiration(TimeSpan duration)
        {
            _expirationInSeconds = (long)duration.TotalSeconds;
            return this;
        }
        
        /// <summary>
        /// Function to use for calculating hashes, by default MD5.
        /// </summary>
        /// <param name="hashFunction">Can be any algorithm supported by HashAlgorithm.Create</param>
        /// <returns>the instance of the builder (to chain operations)</returns>
        public IdempotencyConfigBuilder WithHashFunction(string hashFunction)
        {
            _hashFunction = hashFunction;
            return this;
        }
        
        /// <summary>
        /// Logs to a custom logger.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="log">The logger.</param>
        /// <returns>
        /// The same builder
        /// </returns>
        public IdempotencyConfigBuilder LogTo(ILog log)
        {
            _log = log;
            return this;
        }
    }
}