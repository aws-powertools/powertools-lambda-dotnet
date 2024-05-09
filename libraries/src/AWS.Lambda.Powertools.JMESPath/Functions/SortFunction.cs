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
/// Returns the sorted elements of the input array.
/// </summary>
internal sealed class SortFunction : BaseFunction
{
    internal SortFunction()
        : base(1)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        if (arg0.Type != JmesPathType.Array)
        {
            element = JsonConstants.Null;
            return false;
        }

        if (arg0.GetArrayLength() <= 1)
        {
            element = arg0;
            return true;
        }

        var isNumber1 = arg0[0].Type == JmesPathType.Number;
        var isString1 = arg0[0].Type == JmesPathType.String;
        if (!isNumber1 && !isString1)
        {
            element = JsonConstants.Null;
            return false;
        }

        var comparer = ValueComparer.Instance;

        var list = new List<IValue>();
        foreach (var item in arg0.EnumerateArray())
        {
            var isNumber2 = item.Type == JmesPathType.Number;
            var isString2 = item.Type == JmesPathType.String;
            if (!(isNumber2 == isNumber1 && isString2 == isString1))
            {
                element = JsonConstants.Null;
                return false;
            }

            list.Add(item);
        }

        list.Sort(comparer);
        element = new ArrayValue(list);
        return true;
    }

    public override string ToString()
    {
        return "sort";
    }
}