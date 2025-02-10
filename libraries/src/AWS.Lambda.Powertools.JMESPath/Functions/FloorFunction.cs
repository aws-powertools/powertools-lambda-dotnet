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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Returns the largest integer less than or equal to the argument.
/// </summary>
internal sealed class FloorFunction : BaseFunction
{
    internal FloorFunction()
        : base(1)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var val = args[0];
        if (val.Type != JmesPathType.Number)
        {
            element = JsonConstants.Null;
            return false;
        }

        if (val.TryGetDecimal(out var decVal))
        {
            element = new DecimalValue(decimal.Floor(decVal));
            return true;
        }

        if (val.TryGetDouble(out var dblVal))
        {
            element = new DoubleValue(Math.Floor(dblVal));
            return true;
        }

        element = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "floor";
    }
}