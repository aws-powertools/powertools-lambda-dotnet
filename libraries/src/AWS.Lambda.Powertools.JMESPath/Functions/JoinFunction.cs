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
using System.Text;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class JoinFunction : BaseFunction
{
    internal JoinFunction()
        : base(2)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        var arg1 = args[1];

        if (!(arg0.Type == JmesPathType.String && args[1].Type == JmesPathType.Array))
        {
            element = JsonConstants.Null;
            return false;
        }

        var sep = arg0.GetString();
        var buf = new StringBuilder();
        foreach (var j in arg1.EnumerateArray())
        {
            if (j.Type != JmesPathType.String)
            {
                element = JsonConstants.Null;
                return false;
            }

            if (buf.Length != 0)
            {
                buf.Append(sep);
            }

            var sv = j.GetString();
            buf.Append(sv);
        }

        element = new StringValue(buf.ToString());
        return true;
    }

    public override string ToString()
    {
        return "join";
    }
}