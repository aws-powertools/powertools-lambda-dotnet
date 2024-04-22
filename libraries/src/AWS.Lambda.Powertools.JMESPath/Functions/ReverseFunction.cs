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
using System.Globalization;
using System.Linq;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Returns the elements of the input array in reverse order.
/// </summary>
internal sealed class ReverseFunction : BaseFunction
{
    internal ReverseFunction()
        : base(1)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        switch (arg0.Type)
        {
            case JmesPathType.String:
            {
                element = new StringValue(string.Join("", GraphemeClusters(arg0.GetString()).Reverse().ToArray()));
                return true;
            }
            case JmesPathType.Array:
            {
                var list = new List<IValue>();
                for (var i = arg0.GetArrayLength() - 1; i >= 0; --i)
                {
                    list.Add(arg0[i]);
                }

                element = new ArrayValue(list);
                return true;
            }
            default:
                element = JsonConstants.Null;
                return false;
        }
    }

    private static IEnumerable<string> GraphemeClusters(string s)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(s);
        while (enumerator.MoveNext())
        {
            yield return (string)enumerator.Current;
        }
    }

    public override string ToString()
    {
        return "reverse";
    }
}