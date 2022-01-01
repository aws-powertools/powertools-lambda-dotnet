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

namespace AWS.Lambda.PowerTools.Aspects;

/// <summary>
///     Class UniversalWrapperAttribute.
///     Implements the <see cref="System.Attribute" />
/// </summary>
/// <seealso cref="System.Attribute" />
public abstract class UniversalWrapperAttribute : Attribute
{
    /// <summary>
    ///     Wraps the synchronize.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="target">The target.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="eventArgs">The <see cref="AspectEventArgs" /> instance containing the event data.</param>
    /// <returns>T.</returns>
    protected internal virtual T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
    {
        return target(args);
    }

    /// <summary>
    ///     Wraps the asynchronous.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="target">The target.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="eventArgs">The <see cref="AspectEventArgs" /> instance containing the event data.</param>
    /// <returns>Task&lt;T&gt;.</returns>
    protected internal virtual Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args,
        AspectEventArgs eventArgs)
    {
        return target(args);
    }
}