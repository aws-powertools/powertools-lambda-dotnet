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
/// Returns the string representation of a value.
/// </summary>
internal sealed class ToStringFunction : BaseFunction
{
    internal ToStringFunction()
        : base(1)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        if (args[0].Type == JmesPathType.Expression)
        {
            element = JsonConstants.Null;
            return false;
        }

        var arg0 = args[0];
        switch (arg0.Type)
        {
            case JmesPathType.String:
                element = arg0;
                return true;
            case JmesPathType.Expression:
                element = JsonConstants.Null;
                return false;
            default:
                element = new StringValue(arg0.ToString());
                return true;
        }
    }

    public override string ToString()
    {
        return "to_string";
    }
}