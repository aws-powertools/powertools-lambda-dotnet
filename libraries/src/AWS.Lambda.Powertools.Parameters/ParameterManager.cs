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
    private static ISsmProvider? _ssmProvider;
    private static ISecretsProvider? _secretsProvider;
    private static IAppConfigProvider? _appConfigProvider;
    private static IDynamoDBProvider? _dynamoDBProvider;

    private static ICacheManager? _cache;
    private static ITransformerManager? _transformManager;
    private static TimeSpan? _defaultMaxAge;
    private static ICacheManager Cache => _cache ??= new CacheManager(DateTimeWrapper.Instance);
    private static ITransformerManager TransformManager => _transformManager ??= TransformerManager.Instance;

    public static ISsmProvider SsmProvider => _ssmProvider ??= CreateSsmProvider();
    public static ISecretsProvider SecretsProvider => _secretsProvider ??= CreateSecretsProvider();
    public static IAppConfigProvider AppConfigProvide => _appConfigProvider ??= CreateAppConfigProvider();
    public static IDynamoDBProvider DynamoDBProvider => _dynamoDBProvider ??= CreateDynamoDBProvider();

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

    public static ISsmProvider CreateSsmProvider()
    {
        var provider = new SsmProvider()
            .UseCacheManager(Cache)
            .UseTransformerManager(TransformManager);

        if (_defaultMaxAge.HasValue)
            provider = provider.DefaultMaxAge(_defaultMaxAge.Value);

        return provider;
    }

    public static ISecretsProvider CreateSecretsProvider()
    {
        var provider = new SecretsProvider()
            .UseCacheManager(Cache)
            .UseTransformerManager(TransformManager);

        if (_defaultMaxAge.HasValue)
            provider = provider.DefaultMaxAge(_defaultMaxAge.Value);

        return provider;
    }

    public static IAppConfigProvider CreateAppConfigProvider()
    {
        var provider = new AppConfigProvider()
            .UseCacheManager(Cache)
            .UseTransformerManager(TransformManager);

        if (_defaultMaxAge.HasValue)
            provider = provider.DefaultMaxAge(_defaultMaxAge.Value);

        return provider;
    }

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