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
using System.Reflection;
using System.Threading.Tasks;
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using Newtonsoft.Json.Linq;

namespace AWS.Lambda.Powertools.Idempotency.Internal;

[Aspect(Scope.Global)]
public class IdempotentAspect
{
    private static MethodInfo _asyncErrorHandler = typeof(IdempotentAspect).GetMethod(nameof(WrapAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    [Advice(Kind.Around, Targets = Target.Method)]
    public object Handle(
        [Argument(Source.Target)] Func<object[], object> target,
        [Argument(Source.Arguments)] object[] args,
        [Argument(Source.Instance)] object instance,
        [Argument(Source.ReturnType)] Type retType,
        [Argument(Source.Triggers)] Attribute[] triggers,
        [Argument(Source.Metadata)] MethodBase method
    )
    {
        if (!typeof(Task).IsAssignableFrom((Type?) retType))
        {
            throw new IdempotencyConfigurationException("Invalid Target Exception");
        }

        var syncResultType = retType.IsConstructedGenericType ? retType.GenericTypeArguments[0] : typeof(Task);
        return _asyncErrorHandler.MakeGenericMethod(syncResultType).Invoke(this, new object[] { target, args, method });
    }

    private static async Task<T> WrapAsync<T>(Func<object[], object> target, object[] args, MethodBase method)
    {
        string? idempotencyDisabledEnv = Environment.GetEnvironmentVariable(Constants.IdempotencyDisabledEnv);
        if (idempotencyDisabledEnv is "true")
        {
            return await (Task<T>)target(args);
        }
        JToken payload = JToken.FromObject(args[0]);
        if (payload == null)
        {
            throw new IdempotencyConfigurationException("Unable to get payload from the method. Ensure there is at least one parameter or that you use @IdempotencyKey");
        }

        var types = new[] {typeof(T)};
        var genericType = typeof(IdempotencyHandler<>).MakeGenericType(types);
        var idempotencyHandler = (IdempotencyHandler<T>)Activator.CreateInstance(genericType,target, args, method.Name, payload);
        var result = await idempotencyHandler.Handle();
        return result;
        
    }
}