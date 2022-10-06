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
using System.Threading.Tasks;
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Internal;
using Newtonsoft.Json.Linq;

namespace AWS.Lambda.Powertools.Idempotency;

/// <summary>
/// Idempotent is used to signal that the annotated method is idempotent:
/// Calling this method one or multiple times with the same parameter will always return the same result.
/// This annotation can be placed on any method of a Lambda function
/// <code>
/// [Idempotent]
/// public Task&lt;string&gt; FunctionHandler(string input, ILambdaContext context)
/// {
///  return Task.FromResult(input.ToUpper());
/// }
/// </code>
///     Environment variables                                                                         <br/>
///     ---------------------                                                                         <br/> 
///     <list type="table">
///         <listheader>
///           <term>Variable name</term>
///           <description>Description</description>
///         </listheader>
///         <item>
///             <term>AWS_LAMBDA_FUNCTION_NAME</term>
///             <description>string, function name</description>
///         </item>
///         <item>
///             <term>AWS_REGION</term>
///             <description>string, AWS region</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_IDEMPOTENCY_DISABLED</term>
///             <description>string, Enable or disable the Idempotency</description>
///         </item>
///     </list>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[Injection(typeof(UniversalWrapperAspect), Inherited = true)]
public class IdempotentAttribute : UniversalWrapperAttribute
{
    protected sealed override T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
    {
        throw new IdempotencyConfigurationException("Idempotent attribute can be used on async methods only");
    }

    protected sealed override async Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args,
        AspectEventArgs eventArgs)
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
        var idempotencyHandler = (IdempotencyHandler<T>)Activator.CreateInstance(genericType,target, args, eventArgs.Method.Name, payload);
        var result = await idempotencyHandler.Handle();
        return result;
    }
}