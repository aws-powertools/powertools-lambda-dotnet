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
/// Returns the average of the values
/// </summary>
internal sealed class AvgFunction : BaseFunction
{
    internal AvgFunction()
        : base(1)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        if (arg0.Type != JmesPathType.Array || arg0.GetArrayLength() == 0)
        {
            element = JsonConstants.Null;
            return false;
        }

        if (!SumFunction.Instance.TryEvaluate(resources, args, out var sum))
        {
            element = JsonConstants.Null;
            return false;
        }

        if (sum.TryGetDecimal(out var decVal))
        {
            element = new DecimalValue(decVal / arg0.GetArrayLength());
            return true;
        }

        if (sum.TryGetDouble(out var dblVal))
        {
            element = new DoubleValue(dblVal / arg0.GetArrayLength());
            return true;
        }

        element = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "avg";
    }
}