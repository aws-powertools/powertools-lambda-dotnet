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


using AWS.Lambda.Powertools.Parameters.AppConfig;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.DynamoDB;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Internal.Transform;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.SecretsManager;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters;

/// <summary>
/// Class ParametersManager
/// </summary>
public static class ParametersManager
{
    /// <summary>
    /// Thread-safe lazy initialization of the SsmProvider singleton instance.
    /// Uses LazyThreadSafetyMode.ExecutionAndPublication to ensure only one instance
    /// is created even under concurrent access from multiple threads.
    /// </summary>
    private static readonly Lazy<ISsmProvider> _lazySsmProvider = 
        new Lazy<ISsmProvider>(CreateSsmProvider, LazyThreadSafetyMode.ExecutionAndPublication);
    
    /// <summary>
    /// Thread-safe lazy initialization of the SecretsProvider singleton instance.
    /// Uses LazyThreadSafetyMode.ExecutionAndPublication to ensure only one instance
    /// is created even under concurrent access from multiple threads.
    /// </summary>
    private static readonly Lazy<ISecretsProvider> _lazySecretsProvider = 
        new Lazy<ISecretsProvider>(CreateSecretsProvider, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Thread-safe lazy initialization of the DynamoDBProvider singleton instance.
    /// Uses LazyThreadSafetyMode.ExecutionAndPublication to ensure only one instance
    /// is created even under concurrent access from multiple threads.
    /// </summary>
    private static readonly Lazy<IDynamoDBProvider> _lazyDynamoDBProvider = 
        new Lazy<IDynamoDBProvider>(CreateDynamoDBProvider, LazyThreadSafetyMode.ExecutionAndPublication);
    
    /// <summary>
    /// Thread-safe lazy initialization of the AppConfigProvider singleton instance.
    /// Uses LazyThreadSafetyMode.ExecutionAndPublication to ensure only one instance
    /// is created even under concurrent access from multiple threads.
    /// </summary>
    private static readonly Lazy<IAppConfigProvider> _lazyAppConfigProvider = 
        new Lazy<IAppConfigProvider>(CreateAppConfigProvider, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// The CacheManager instance. Access is protected by _configLock.
    /// </summary>
    private static ICacheManager? _cache;
    
    /// <summary>
    /// The TransformerManager instance. Access is protected by _configLock.
    /// </summary>
    private static ITransformerManager? _transformManager;
    
    /// <summary>
    /// The DefaultMaxAge across all providers. Access is protected by _configLock.
    /// </summary>
    private static TimeSpan? _defaultMaxAge;
    
    /// <summary>
    /// Lock object for thread-safe configuration operations.
    /// </summary>
    private static readonly object _configLock = new object();
    
    /// <summary>
    /// Gets the CacheManager instance.
    /// </summary>
    /// <value>The CacheManager instance.</value>
    private static ICacheManager Cache => _cache ?? (_cache = new CacheManager(DateTimeWrapper.Instance));
    
    /// <summary>
    /// Gets the TransformerManager instance.
    /// </summary>
    /// <value>The TransformerManager instance.</value>
    private static ITransformerManager TransformManager => _transformManager ?? (_transformManager = TransformerManager.Instance);

    /// <summary>
    /// Gets the SsmProvider instance in a thread-safe manner.
    /// </summary>
    /// <value>The SsmProvider instance.</value>
    public static ISsmProvider SsmProvider => _lazySsmProvider.Value;
    
    /// <summary>
    /// Gets the SecretsProvider instance in a thread-safe manner.
    /// </summary>
    /// <value>The SecretsProvider instance.</value>
    public static ISecretsProvider SecretsProvider => _lazySecretsProvider.Value;

    /// <summary>
    /// Gets the DynamoDBProvider instance in a thread-safe manner.
    /// </summary>
    /// <value>The DynamoDBProvider instance.</value>
    public static IDynamoDBProvider DynamoDBProvider => _lazyDynamoDBProvider.Value;

    /// <summary>
    /// Gets the AppConfigProvider instance in a thread-safe manner.
    /// </summary>
    /// <value>The AppConfigProvider instance.</value>
    public static IAppConfigProvider AppConfigProvider => _lazyAppConfigProvider.Value;

    /// <summary>
    /// Set the caching default maximum age for all providers.
    /// Thread-safe: uses lock to ensure consistent configuration across concurrent calls.
    /// </summary>
    /// <param name="maxAge">The maximum age.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">maxAge</exception>
    public static void DefaultMaxAge(TimeSpan maxAge)
    {
        if (maxAge <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxAge),
                "The value for maximum age must be greater than zero.");
        
        lock (_configLock)
        {
            _defaultMaxAge = maxAge;
            
            // Apply to existing providers if they have been initialized
            if (_lazySsmProvider.IsValueCreated)
                _lazySsmProvider.Value.DefaultMaxAge(maxAge);
            if (_lazySecretsProvider.IsValueCreated)
                _lazySecretsProvider.Value.DefaultMaxAge(maxAge);
            if (_lazyDynamoDBProvider.IsValueCreated)
                _lazyDynamoDBProvider.Value.DefaultMaxAge(maxAge);
            if (_lazyAppConfigProvider.IsValueCreated)
                _lazyAppConfigProvider.Value.DefaultMaxAge(maxAge);
        }
    }

    /// <summary>
    /// Set the CacheManager instance for all providers.
    /// Thread-safe: uses lock to ensure consistent configuration across concurrent calls.
    /// </summary>
    /// <param name="cacheManager">The CacheManager instance.</param>
    public static void UseCacheManager(ICacheManager cacheManager)
    {
        lock (_configLock)
        {
            _cache = cacheManager;
            
            // Apply to existing providers if they have been initialized
            if (_lazySsmProvider.IsValueCreated)
                _lazySsmProvider.Value.UseCacheManager(cacheManager);
            if (_lazySecretsProvider.IsValueCreated)
                _lazySecretsProvider.Value.UseCacheManager(cacheManager);
            if (_lazyDynamoDBProvider.IsValueCreated)
                _lazyDynamoDBProvider.Value.UseCacheManager(cacheManager);
            if (_lazyAppConfigProvider.IsValueCreated)
                _lazyAppConfigProvider.Value.UseCacheManager(cacheManager);
        }
    }

    /// <summary>
    /// Set the TransformerManager instance for all providers.
    /// Thread-safe: uses lock to ensure consistent configuration across concurrent calls.
    /// </summary>
    /// <param name="transformerManager">The TransformerManager instance.</param>
    public static void UseTransformerManager(ITransformerManager transformerManager)
    {
        lock (_configLock)
        {
            _transformManager = transformerManager;
            
            // Apply to existing providers if they have been initialized
            if (_lazySsmProvider.IsValueCreated)
                _lazySsmProvider.Value.UseTransformerManager(transformerManager);
            if (_lazySecretsProvider.IsValueCreated)
                _lazySecretsProvider.Value.UseTransformerManager(transformerManager);
            if (_lazyDynamoDBProvider.IsValueCreated)
                _lazyDynamoDBProvider.Value.UseTransformerManager(transformerManager);
            if (_lazyAppConfigProvider.IsValueCreated)
                _lazyAppConfigProvider.Value.UseTransformerManager(transformerManager);
        }
    }

    /// <summary>
    /// Registers a new transformer instance by name for all providers.
    /// Thread-safe: uses lock to ensure consistent configuration across concurrent calls.
    /// </summary>
    /// <param name="name">The transformer unique name.</param>
    /// <param name="transformer">The transformer instance.</param>
    public static void AddTransformer(string name, ITransformer transformer)
    {
        lock (_configLock)
        {
            TransformManager.AddTransformer(name, transformer);
            
            // Apply to existing providers if they have been initialized
            if (_lazySsmProvider.IsValueCreated)
                _lazySsmProvider.Value.AddTransformer(name, transformer);
            if (_lazySecretsProvider.IsValueCreated)
                _lazySecretsProvider.Value.AddTransformer(name, transformer);
            if (_lazyDynamoDBProvider.IsValueCreated)
                _lazyDynamoDBProvider.Value.AddTransformer(name, transformer);
            if (_lazyAppConfigProvider.IsValueCreated)
                _lazyAppConfigProvider.Value.AddTransformer(name, transformer);
        }
    }
    
    /// <summary>
    /// Configure the transformer to raise exception on transformation error.
    /// Thread-safe: uses lock to ensure consistent configuration across concurrent calls.
    /// </summary>
    public static void RaiseTransformationError()
    {
        lock (_configLock)
        {
            // Apply to existing providers if they have been initialized
            if (_lazySsmProvider.IsValueCreated)
                _lazySsmProvider.Value.RaiseTransformationError();
            if (_lazySecretsProvider.IsValueCreated)
                _lazySecretsProvider.Value.RaiseTransformationError();
            if (_lazyDynamoDBProvider.IsValueCreated)
                _lazyDynamoDBProvider.Value.RaiseTransformationError();
            if (_lazyAppConfigProvider.IsValueCreated)
                _lazyAppConfigProvider.Value.RaiseTransformationError();
        }
    }
    
    /// <summary>
    /// Configure the transformer to raise exception or return Null on transformation error.
    /// Thread-safe: uses lock to ensure consistent configuration across concurrent calls.
    /// </summary>
    /// <param name="raiseError">true for raise error, false for return Null.</param>
    public static void RaiseTransformationError(bool raiseError)
    {
        lock (_configLock)
        {
            // Apply to existing providers if they have been initialized
            if (_lazySsmProvider.IsValueCreated)
                _lazySsmProvider.Value.RaiseTransformationError(raiseError);
            if (_lazySecretsProvider.IsValueCreated)
                _lazySecretsProvider.Value.RaiseTransformationError(raiseError);
            if (_lazyDynamoDBProvider.IsValueCreated)
                _lazyDynamoDBProvider.Value.RaiseTransformationError(raiseError);
            if (_lazyAppConfigProvider.IsValueCreated)
                _lazyAppConfigProvider.Value.RaiseTransformationError(raiseError);
        }
    }

    /// <summary>
    /// Create a new instance of SsmProvider.
    /// </summary>
    /// <value>The SsmProvider instance.</value>
    public static ISsmProvider CreateSsmProvider()
    {
        var provider = new SsmProvider()
            .UseCacheManager(Cache)
            .UseTransformerManager(TransformManager);

        if (_defaultMaxAge.HasValue)
            provider = provider.DefaultMaxAge(_defaultMaxAge.Value);

        return provider;
    }

    /// <summary>
    /// Create a new instance of SecretsProvider.
    /// </summary>
    /// <value>The SecretsProvider instance.</value>
    public static ISecretsProvider CreateSecretsProvider()
    {
        var provider = new SecretsProvider()
            .UseCacheManager(Cache)
            .UseTransformerManager(TransformManager);

        if (_defaultMaxAge.HasValue)
            provider = provider.DefaultMaxAge(_defaultMaxAge.Value);

        return provider;
    }

    /// <summary>
    /// Create a new instance of DynamoDBProvider.
    /// </summary>
    /// <value>The DynamoDBProvider instance.</value>
    public static IDynamoDBProvider CreateDynamoDBProvider()
    {
        var provider = new DynamoDBProvider()
            .UseCacheManager(Cache)
            .UseTransformerManager(TransformManager);

        if (_defaultMaxAge.HasValue)
            provider = provider.DefaultMaxAge(_defaultMaxAge.Value);

        return provider;
    }
    
    /// <summary>
    /// Create a new instance of AppConfigProvider.
    /// </summary>
    /// <value>The AppConfigProvider instance.</value>
    public static IAppConfigProvider CreateAppConfigProvider()
    {
        var provider = new AppConfigProvider()
            .UseCacheManager(Cache)
            .UseTransformerManager(TransformManager);

        if (_defaultMaxAge.HasValue)
            provider = provider.DefaultMaxAge(_defaultMaxAge.Value);

        return provider;
    }
}