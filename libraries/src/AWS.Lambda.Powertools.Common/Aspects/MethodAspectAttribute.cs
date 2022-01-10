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

namespace AWS.Lambda.Powertools.Common;

/// <summary>
///     Class MethodAspectAttribute.
///     Implements the <see cref="UniversalWrapperAttribute" />
/// </summary>
/// <seealso cref="UniversalWrapperAttribute" />
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[Injection(typeof(UniversalWrapperAspect), Inherited = true)]
public abstract class MethodAspectAttribute : UniversalWrapperAttribute
{
    /// <summary>
    ///     The aspect handler
    /// </summary>
    private IMethodAspectHandler _aspectHandler;

    /// <summary>
    ///     Gets the aspect handler.
    /// </summary>
    /// <value>The aspect handler.</value>
    private IMethodAspectHandler AspectHandler => _aspectHandler ??= CreateHandler();

    /// <summary>
    ///     Creates the handler.
    /// </summary>
    /// <returns>IMethodAspectHandler.</returns>
    protected abstract IMethodAspectHandler CreateHandler();

    /// <summary>
    ///     Wraps as a synchronous operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="target">The target.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="eventArgs">The <see cref="AspectEventArgs" /> instance containing the event data.</param>
    /// <returns>T.</returns>
    protected internal sealed override T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
    {
        AspectHandler.OnEntry(eventArgs);
        try
        {
            var result = base.WrapSync(target, args, eventArgs);
            AspectHandler.OnSuccess(eventArgs, result);
            return result;
        }
        catch (Exception exception)
        {
            return AspectHandler.OnException<T>(eventArgs, exception);
        }
        finally
        {
            AspectHandler.OnExit(eventArgs);
        }
    }

    /// <summary>
    ///     Wrap as an asynchronous operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="target">The target.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="eventArgs">The <see cref="AspectEventArgs" /> instance containing the event data.</param>
    /// <returns>A Task&lt;T&gt; representing the asynchronous operation.</returns>
    protected internal sealed override async Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args,
        AspectEventArgs eventArgs)
    {
        AspectHandler.OnEntry(eventArgs);
        try
        {
            var result = await base.WrapAsync(target, args, eventArgs);
            AspectHandler.OnSuccess(eventArgs, result);
            return result;
        }
        catch (Exception exception)
        {
            return AspectHandler.OnException<T>(eventArgs, exception);
        }
        finally
        {
            AspectHandler.OnExit(eventArgs);
        }
    }
}
