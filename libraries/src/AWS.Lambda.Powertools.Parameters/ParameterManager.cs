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

public static class ParameterManager
{
    private static SsmProvider? _ssmProvider;
    private static SecretsProvider? _secretsProvider;
    private static AppConfigProvider? _appConfigProvider;
    private static DynamoDBProvider? _dynamoDBProvider;

    private static ICacheManager? _cache;
    private static ITransformerManager? _transformManager;
    private static TimeSpan? _defaultMaxAge;
    private static ICacheManager Cache => _cache ??= new CacheManager(DateTimeWrapper.Instance);
    private static ITransformerManager TransformManager => _transformManager ??= TransformerManager.Instance;

    public static void DefaultMaxAge(TimeSpan maxAge)
    {
        _defaultMaxAge = maxAge;
        _ssmProvider?.DefaultMaxAge(maxAge);
        _secretsProvider?.DefaultMaxAge(maxAge);
        _appConfigProvider?.DefaultMaxAge(maxAge);
        _dynamoDBProvider?.DefaultMaxAge(maxAge);
    }

    public static void UseCacheManager(ICacheManager cacheManager)
    {
        _cache = cacheManager;
        _ssmProvider?.UseCacheManager(cacheManager);
        _secretsProvider?.UseCacheManager(cacheManager);
        _appConfigProvider?.UseCacheManager(cacheManager);
        _dynamoDBProvider?.UseCacheManager(cacheManager);
    }

    public static void UseTransformerManager(ITransformerManager transformerManager)
    {
        _transformManager = transformerManager;
        _ssmProvider?.UseTransformerManager(transformerManager);
        _secretsProvider?.UseTransformerManager(transformerManager);
        _appConfigProvider?.UseTransformerManager(transformerManager);
        _dynamoDBProvider?.UseTransformerManager(transformerManager);
    }

    public static void AddTransformer(string name, ITransformer transformer)
    {
        TransformManager.AddTransformer(name, transformer);
        _ssmProvider?.AddTransformer(name, transformer);
        _secretsProvider?.AddTransformer(name, transformer);
        _appConfigProvider?.AddTransformer(name, transformer);
        _dynamoDBProvider?.AddTransformer(name, transformer);
    }

    public static SsmProvider SsmProvider
    {
        get
        {
            if (_ssmProvider is not null)
                return _ssmProvider;

            _ssmProvider = new SsmProvider()
                .UseCacheManager(Cache)
                .UseTransformerManager(TransformManager);

            if (_defaultMaxAge.HasValue)
                _ssmProvider = _ssmProvider.DefaultMaxAge(_defaultMaxAge.Value);

            return _ssmProvider;
        }
    }

    public static SecretsProvider SecretsProvider
    {
        get
        {
            if (_secretsProvider is not null)
                return _secretsProvider;

            _secretsProvider = new SecretsProvider()
                .UseCacheManager(Cache)
                .UseTransformerManager(TransformManager);

            if (_defaultMaxAge.HasValue)
                _secretsProvider = _secretsProvider.DefaultMaxAge(_defaultMaxAge.Value);

            return _secretsProvider;
        }
    }

    public static AppConfigProvider AppConfigProvider
    {
        get
        {
            if (_appConfigProvider is not null)
                return _appConfigProvider;

            _appConfigProvider = new AppConfigProvider()
                .UseCacheManager(Cache)
                .UseTransformerManager(TransformManager);

            if (_defaultMaxAge.HasValue)
                _appConfigProvider = _appConfigProvider.DefaultMaxAge(_defaultMaxAge.Value);

            return _appConfigProvider;
        }
    }

    public static DynamoDBProvider DynamoDBProvider
    {
        get
        {
            if (_dynamoDBProvider is not null)
                return _dynamoDBProvider;

            _dynamoDBProvider = new DynamoDBProvider()
                .UseCacheManager(Cache)
                .UseTransformerManager(TransformManager);

            if (_defaultMaxAge.HasValue)
                _dynamoDBProvider = _dynamoDBProvider.DefaultMaxAge(_defaultMaxAge.Value);

            return _dynamoDBProvider;
        }
    }
}