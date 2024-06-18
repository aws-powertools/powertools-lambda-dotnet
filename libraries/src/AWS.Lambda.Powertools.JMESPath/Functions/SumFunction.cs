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
/// Returns the sum of the values in a list.
/// </summary>
internal sealed class SumFunction : BaseFunction
{
    internal static SumFunction Instance { get; } = new();

    internal SumFunction()
        : base(1)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        if (arg0.Type != JmesPathType.Array)
        {
            element = JsonConstants.Null;
            return false;
        }

        foreach (var item in arg0.EnumerateArray())
        {
            if (item.Type == JmesPathType.Number) continue;
            element = JsonConstants.Null;
            return false;
        }

        var success = true;
        decimal decSum = 0;
        foreach (var item in arg0.EnumerateArray())
        {
            if (!item.TryGetDecimal(out var dec))
            {
                success = false;
                break;
            }

            decSum += dec;
        }

        if (success)
        {
            element = new DecimalValue(decSum);
            return true;
        }

        double dblSum = 0;
        foreach (var item in arg0.EnumerateArray())
        {
            if (!item.TryGetDouble(out var dbl))
            {
                element = JsonConstants.Null;
                return false;
            }

            dblSum += dbl;
        }

        element = new DoubleValue(dblSum);
        return true;
    }

    public override string ToString()
    {
        return "sum";
    }
}