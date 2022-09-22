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
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.SecretsManager;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters;

public static class ParameterManager
{
    private static SsmProvider? _ssmProvider;
    private static SecretsProvider? _secretsProvider;

    private static ICacheManager? _cache;
    private static ITransformerManager? _transformManager;
    private static TimeSpan _defaultMaxAge = CacheManager.DefaultMaxAge;
    private static ICacheManager Cache => _cache ??= new CacheManager(DateTimeWrapper.Instance);
    private static ITransformerManager TransformManager => _transformManager ??= TransformerManager.Instance;

    public static void DefaultMaxAge(TimeSpan maxAge)
    {
        _defaultMaxAge = maxAge;
        _ssmProvider?.DefaultMaxAge(maxAge);
        _secretsProvider?.DefaultMaxAge(maxAge);
    }

    public static void UseCacheManager(ICacheManager cacheManager)
    {
        _cache = cacheManager;
        _ssmProvider?.UseCacheManager(cacheManager);
        _secretsProvider?.UseCacheManager(cacheManager);
    }

    public static void UseTransformerManager(ITransformerManager transformerManager)
    {
        _transformManager = transformerManager;
        _ssmProvider?.UseTransformerManager(transformerManager);
        _secretsProvider?.UseTransformerManager(transformerManager);
    }

    public static void AddTransformer(string name, ITransformer transformer)
    {
        TransformManager.AddTransformer(name, transformer);
        _ssmProvider?.AddTransformer(name, transformer);
        _secretsProvider?.AddTransformer(name, transformer);
    }

    public static SsmProvider SsmProvider =>
        _ssmProvider ??= new SsmProvider()
            .DefaultMaxAge(_defaultMaxAge)
            .UseCacheManager(Cache)
            .UseTransformerManager(TransformManager);

    public static SecretsProvider SecretsProvider =>
        _secretsProvider ??= new SecretsProvider()
            .DefaultMaxAge(_defaultMaxAge)
            .UseCacheManager(Cache)
            .UseTransformerManager(TransformManager);
}