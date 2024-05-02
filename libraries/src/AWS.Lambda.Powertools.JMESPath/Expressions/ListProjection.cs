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
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Expressions;

/// <summary>
/// Represents a projection that returns a list of values.
/// </summary>
internal sealed class ListProjection : Projection
{    
    internal ListProjection()
        : base(Operator.Projection)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources,
        IValue current, 
        out IValue value)
    {
        if (current.Type != JmesPathType.Array)
        {
            value = JsonConstants.Null;
            return true;
        }

        var result = new List<IValue>();
        foreach (var item in current.EnumerateArray())
        {
            if (item.Type != JmesPathType.Null)
            {
                if (!TryApplyExpressions(resources, item, out var val))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                if (val.Type != JmesPathType.Null)
                {
                    result.Add(val);
                }
            }
        }
        value = new ArrayValue(result);
        return true;
    }

    public override string ToString()
    {
        return "ListProjection";
    }
}