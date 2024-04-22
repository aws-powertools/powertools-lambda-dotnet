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

using System.Collections.Generic;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Base class for JMESPath functions.
/// </summary>
internal abstract class BaseFunction : IFunction
{
    private protected BaseFunction(int? argCount)
    {
        Arity = argCount;
    }

    /// <inheritdoc />
    public int? Arity { get; }

    /// <inheritdoc />
    public abstract bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element);
}