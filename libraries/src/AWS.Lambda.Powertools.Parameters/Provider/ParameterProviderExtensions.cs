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
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Provider;

public static class ParameterProviderExtensions
{
    public static TProvider DefaultMaxAge<TProvider>(this TProvider instance, TimeSpan maxAge)
        where TProvider : IParameterProviderBase
    {
        ((ParameterProviderBase)(object)instance).Handler.SetDefaultMaxAge(maxAge);
        return instance;
    }

    public static TProvider UseCacheManager<TProvider>(this TProvider instance, ICacheManager cacheManager)
        where TProvider : IParameterProviderBase
    {
        ((ParameterProviderBase)(object)instance).Handler.SetCacheManager(cacheManager);
        return instance;
    }

    public static TProvider UseTransformerManager<TProvider>(this TProvider instance,
        ITransformerManager transformerManager)
        where TProvider : IParameterProviderBase
    {
        ((ParameterProviderBase)(object)instance).Handler.SetTransformerManager(transformerManager);
        return instance;
    }

    public static TProvider AddTransformer<TProvider>(this TProvider instance, string name, ITransformer transformer)
        where TProvider : IParameterProviderBase
    {
        ((ParameterProviderBase)(object)instance).Handler.AddCustomTransformer(name, transformer);
        return instance;
    }
    
    public static TProvider RaiseTransformationError<TProvider>(this TProvider instance)
        where TProvider : IParameterProviderBase
    {
        RaiseTransformationError(instance, true);
        return instance;
    }
    
    public static TProvider RaiseTransformationError<TProvider>(this TProvider instance, bool raiseError)
        where TProvider : IParameterProviderBase
    {
        ((ParameterProviderBase)(object)instance).Handler.SetRaiseTransformationError(raiseError);
        return instance;
    }
}