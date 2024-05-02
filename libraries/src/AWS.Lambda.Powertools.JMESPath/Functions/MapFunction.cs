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
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Returns a new array with the results of calling a provided function on every element in the calling array.
/// </summary>
internal sealed class MapFunction : BaseFunction
{
    internal MapFunction()
        : base(2)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        if (!(args[0].Type == JmesPathType.Expression && args[1].Type == JmesPathType.Array))
        {
            element = JsonConstants.Null;
            return false;
        }

        var expr = args[0].GetExpression();
        var arg0 = args[1];

        var list = new List<IValue>();

        foreach (var item in arg0.EnumerateArray())
        {
            if (!expr.TryEvaluate(resources, item, out var val))
            {
                element = JsonConstants.Null;
                return false;
            }

            list.Add(val);
        }

        element = new ArrayValue(list);
        return true;
    }

    public override string ToString()
    {
        return "map";
    }
}