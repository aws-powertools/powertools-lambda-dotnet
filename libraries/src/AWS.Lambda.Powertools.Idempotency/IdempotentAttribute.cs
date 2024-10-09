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
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Internal;
using AWS.Lambda.Powertools.Idempotency.Internal.Serializers;

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
    /// <summary>
    ///     Wraps as a synchronous operation, simply throws IdempotencyConfigurationException
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="target">The target.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="eventArgs">The <see cref="AspectEventArgs" /> instance containing the event data.</param>
    /// <returns>T.</returns>
    protected internal sealed override T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
    {
        if (PowertoolsConfigurations.Instance.IdempotencyDisabled)
        {
            return base.WrapSync(target, args, eventArgs);
        }
        if (eventArgs.ReturnType == typeof(void))
        {
            throw new IdempotencyConfigurationException("The annotated method doesn't return anything. Unable to perform idempotency on void return type");
        }

        var payload = GetPayload<T>(eventArgs);
        if (payload == null)
        {
            throw new IdempotencyConfigurationException("Unable to get payload from the method. Ensure there is at least one parameter or that you use @IdempotencyKey");
        }

        Task<T> ResultDelegate() => Task.FromResult(target(args));

        var idempotencyHandler = new IdempotencyAspectHandler<T>(ResultDelegate, eventArgs.Method.Name, payload,GetContext(eventArgs));
        if (idempotencyHandler == null)
        {
            throw new Exception("Failed to create an instance of IdempotencyAspectHandler");
        }
        var result = idempotencyHandler.Handle().GetAwaiter().GetResult();
        return result;
    }

    /// <summary>
    ///     Wrap as an asynchronous operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="target">The target.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="eventArgs">The <see cref="AspectEventArgs" /> instance containing the event data.</param>
    /// <returns>A Task&lt;T&gt; representing the asynchronous operation.</returns>
    protected internal sealed override async Task<T> WrapAsync<T>(
        Func<object[], Task<T>> target, object[] args, AspectEventArgs eventArgs)
    {
        if (PowertoolsConfigurations.Instance.IdempotencyDisabled)
        {
            return await base.WrapAsync(target, args, eventArgs);
        }

        if (eventArgs.ReturnType == typeof(void))
        {
            throw new IdempotencyConfigurationException("The annotated method doesn't return anything. Unable to perform idempotency on void return type");
        }
        
        var payload = GetPayload<T>(eventArgs);
        if (payload == null)
        {
            throw new IdempotencyConfigurationException("Unable to get payload from the method. Ensure there is at least one parameter or that you use @IdempotencyKey");
        }
        
        Task<T> ResultDelegate() => target(args);
        
        var idempotencyHandler = new IdempotencyAspectHandler<T>(ResultDelegate, eventArgs.Method.Name, payload, GetContext(eventArgs));
        if (idempotencyHandler == null)
        {
            throw new Exception("Failed to create an instance of IdempotencyAspectHandler");
        }
        var result = await idempotencyHandler.Handle();
        return result;
    }
    
    /// <summary>
    /// Retrieve the payload from the annotated method parameter
    /// </summary>
    /// <param name="eventArgs">The <see cref="AspectEventArgs" /> instance containing the event data.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>The payload</returns>
    private static JsonDocument GetPayload<T>(AspectEventArgs eventArgs)
    {
        JsonDocument payload = null;
        var eventArgsMethod = eventArgs.Method;
        var args = eventArgs.Args;
        var isPlacedOnRequestHandler = IsPlacedOnRequestHandler(eventArgsMethod);
        // Use the first argument if IdempotentAttribute placed on handler or number of arguments is 1
        if (isPlacedOnRequestHandler || args.Count == 1)
        {
            payload = args is not null && args.Any() ? JsonDocument.Parse(IdempotencySerializer.Serialize(args[0], typeof(object))) : null;
        }
        else
        {
            //Find the first parameter in eventArgsMethod with attribute IdempotencyKeyAttribute
            var parameter = eventArgsMethod.GetParameters().FirstOrDefault(p => p.GetCustomAttribute<IdempotencyKeyAttribute>() != null);
            if (parameter != null)
            {
                // set payload to the value of the parameter
                payload = JsonDocument.Parse(IdempotencySerializer.Serialize(args[Array.IndexOf(eventArgsMethod.GetParameters(), parameter)], typeof(object)));
            }
        }

        return payload;
    }

    private static bool IsPlacedOnRequestHandler(MethodBase method)
    {
        //Check if method has two arguments and the second one is of type ILambdaContext
        return method.GetParameters().Length == 2 && method.GetParameters()[1].ParameterType == typeof(ILambdaContext);
    }
    
    private static ILambdaContext GetContext(AspectEventArgs args)
    {
        if (IsPlacedOnRequestHandler(args.Method))
        {
            return (ILambdaContext)args.Args[1];
        }
        
        return Idempotency.Instance.LambdaContext;
    }
}