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
using System.Diagnostics;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Returns the input array sorted by the value of the expression by resources.
/// </summary>
internal sealed class SortByFunction : BaseFunction
{
    internal SortByFunction()
        : base(2)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        if (!(args[0].Type == JmesPathType.Array && args[1].Type == JmesPathType.Expression))
        {
            element = JsonConstants.Null;
            return false;
        }

        var arg0 = args[0];
        if (arg0.GetArrayLength() <= 1)
        {
            element = arg0;
            return true;
        }

        var expr = args[1].GetExpression();

        var list = new List<IValue>();
        foreach (var item in arg0.EnumerateArray())
        {
            list.Add(item);
        }

        var comparer = new SortByComparer(resources, expr);
        list.Sort(comparer);
        if (comparer.IsValid)
        {
            element = new ArrayValue(list);
            return true;
        }

        element = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "sort_by";
    }
}