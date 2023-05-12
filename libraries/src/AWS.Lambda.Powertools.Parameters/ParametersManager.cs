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
    /// The SsmProvider instance
    /// </summary>
    private static ISsmProvider? _ssmProvider;
    
    /// <summary>
    /// The SecretsProvider instance
    /// </summary>
    private static ISecretsProvider? _secretsProvider;

    /// <summary>
    /// The DynamoDBProvider instance
    /// </summary>
    private static IDynamoDBProvider? _dynamoDBProvider;

    /// <summary>
    /// The CacheManager instance
    /// </summary>
    private static ICacheManager? _cache;
    
    /// <summary>
    /// The TransformerManager instance
    /// </summary>
    private static ITransformerManager? _transformManager;
    
    /// <summary>
    /// The DefaultMaxAge across all providers
    /// </summary>
    private static TimeSpan? _defaultMaxAge;
    
    /// <summary>
    /// Gets the CacheManager instance.
    /// </summary>
    /// <value>The CacheManager instance.</value>
    private static ICacheManager Cache => _cache ??= new CacheManager(DateTimeWrapper.Instance);
    
    /// <summary>
    /// Gets the TransformerManager instance.
    /// </summary>
    /// <value>The TransformerManager instance.</value>
    private static ITransformerManager TransformManager => _transformManager ??= TransformerManager.Instance;

    /// <summary>
    /// Gets the SsmProvider instance.
    /// </summary>
    /// <value>The SsmProvider instance.</value>
    public static ISsmProvider SsmProvider => _ssmProvider ??= CreateSsmProvider();
    
    /// <summary>
    /// Gets the SecretsProvider instance.
    /// </summary>
    /// <value>The SecretsProvider instance.</value>
    public static ISecretsProvider SecretsProvider => _secretsProvider ??= CreateSecretsProvider();

    /// <summary>
    /// Gets the DynamoDBProvider instance.
    /// </summary>
    /// <value>The DynamoDBProvider instance.</value>
    public static IDynamoDBProvider DynamoDBProvider => _dynamoDBProvider ??= CreateDynamoDBProvider();

    /// <summary>
    /// Set the caching default maximum age for all providers.
    /// </summary>
    /// <param name="maxAge">The maximum age.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">maxAge</exception>
    public static void DefaultMaxAge(TimeSpan maxAge)
    {
        if (maxAge <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxAge),
                "The value for maximum age must be greater than zero.");
            
        _defaultMaxAge = maxAge;
        _ssmProvider?.DefaultMaxAge(maxAge);
        _secretsProvider?.DefaultMaxAge(maxAge);
        _dynamoDBProvider?.DefaultMaxAge(maxAge);
    }

    /// <summary>
    /// Set the CacheManager instance for all providers.
    /// </summary>
    /// <param name="cacheManager">The CacheManager instance.</param>
    public static void UseCacheManager(ICacheManager cacheManager)
    {
        _cache = cacheManager;
        _ssmProvider?.UseCacheManager(cacheManager);
        _secretsProvider?.UseCacheManager(cacheManager);
        _dynamoDBProvider?.UseCacheManager(cacheManager);
    }

    /// <summary>
    /// Set the TransformerManager instance for all providers.
    /// </summary>
    /// <param name="transformerManager">The TransformerManager instance.</param>
    public static void UseTransformerManager(ITransformerManager transformerManager)
    {
        _transformManager = transformerManager;
        _ssmProvider?.UseTransformerManager(transformerManager);
        _secretsProvider?.UseTransformerManager(transformerManager);
        _dynamoDBProvider?.UseTransformerManager(transformerManager);
    }

    /// <summary>
    /// Registers a new transformer instance by name for all providers.
    /// </summary>
    /// <param name="name">The transformer unique name.</param>
    /// <param name="transformer">The transformer instance.</param>
    public static void AddTransformer(string name, ITransformer transformer)
    {
        TransformManager.AddTransformer(name, transformer);
        _ssmProvider?.AddTransformer(name, transformer);
        _secretsProvider?.AddTransformer(name, transformer);
        _dynamoDBProvider?.AddTransformer(name, transformer);
    }
    
    /// <summary>
    /// Configure the transformer to raise exception on transformation error
    /// </summary>
    public static void RaiseTransformationError()
    {
        _ssmProvider?.RaiseTransformationError();
        _secretsProvider?.RaiseTransformationError();
        _dynamoDBProvider?.RaiseTransformationError();
    }
    
    /// <summary>
    /// Configure the transformer to raise exception or return Null on transformation error
    /// </summary>
    /// <param name="raiseError">true for raise error, false for return Null.</param>
    public static void RaiseTransformationError(bool raiseError)
    {
        _ssmProvider?.RaiseTransformationError(raiseError);
        _secretsProvider?.RaiseTransformationError(raiseError);
        _dynamoDBProvider?.RaiseTransformationError(raiseError);
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
}