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
/// Represents the projection of an object.
/// </summary>
internal sealed class ObjectProjection : Projection
{
    internal ObjectProjection()
        : base(Operator.Projection)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources,
        IValue current, 
        out IValue value)
    {
        if (current.Type != JmesPathType.Object)
        {
            value = JsonConstants.Null;
            return true;
        }

        var result = new List<IValue>();
        value = new ArrayValue(result);
        foreach (var item in current.EnumerateObject())
        {
            if (item.Value.Type == JmesPathType.Null) continue;
            if (!TryApplyExpressions(resources, item.Value, out var val))
            {
                return false;
            }
            if (val.Type != JmesPathType.Null)
            {
                result.Add(val);
            }
        }
        return true;
    }

    public override string ToString()
    {
        return "ObjectProjection";
    }
}