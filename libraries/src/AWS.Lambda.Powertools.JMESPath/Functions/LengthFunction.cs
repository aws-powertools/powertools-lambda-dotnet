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
using System.Text;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Returns the number of elements in a value.
/// </summary>
internal sealed class LengthFunction : BaseFunction
{
    internal LengthFunction()
        : base(1)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];

        switch (arg0.Type)
        {
            case JmesPathType.Object:
            {
                var count = 0;
                foreach (var unused in arg0.EnumerateObject())
                {
                    ++count;
                }

                element = new DecimalValue(new decimal(count));
                return true;
            }
            case JmesPathType.Array:
                element = new DecimalValue(new decimal(arg0.GetArrayLength()));
                return true;
            case JmesPathType.String:
            {
                var bytes = Encoding.UTF32.GetBytes(arg0.GetString().ToCharArray());
                element = new DecimalValue(new decimal(bytes.Length / 4));
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
        return "length";
    }
}