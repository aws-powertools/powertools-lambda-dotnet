/*
  * Copyright JsonCons.Net authors. All Rights Reserved.
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
using System.Diagnostics;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Returns true if the first argument starts with the second argument.
/// </summary>
internal sealed class StartsWithFunction : BaseFunction
{
    internal StartsWithFunction()
        : base(2)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        // 2024-04-19: Powertools addition.
        return EvaluateStartEndWith.TryEvaluate(args, out element, s0 => s0.StartsWith);
    }

    public override string ToString()
    {
        return "starts_with";
    }
}