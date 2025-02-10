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
using System.Linq;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Returns true if the first argument contains the second argument.
/// </summary>
internal sealed class ContainsFunction : BaseFunction
{
    internal ContainsFunction()
        : base(2)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        var arg1 = args[1];

        var comparer = ValueEqualityComparer.Instance;

        switch (arg0.Type)
        {
            case JmesPathType.Array:
                if (arg0.EnumerateArray().Any(item => comparer.Equals(item, arg1)))
                {
                    element = JsonConstants.True;
                    return true;
                }

                element = JsonConstants.False;
                return true;
            case JmesPathType.String:
            {
                if (arg1.Type != JmesPathType.String)
                {
                    element = JsonConstants.Null;
                    return false;
                }

                var s0 = arg0.GetString();
                var s1 = arg1.GetString();
                if (s0.Contains(s1))
                {
                    element = JsonConstants.True;
                    return true;
                }

                element = JsonConstants.False;
                return true;
            }
            default:
            {
                element = JsonConstants.Null;
                return false;
            }
        }
    }

    public override string ToString()
    {
        return "contains";
    }
}