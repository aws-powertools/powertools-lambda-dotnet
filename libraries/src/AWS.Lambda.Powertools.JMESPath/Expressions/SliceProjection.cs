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
/// Represents a projection of a slice of an array.
/// </summary>
internal sealed class SliceProjection : Projection
{
    /// <summary>
    /// The slice to project.
    /// </summary>
    private readonly Slice _slice;
    
    internal SliceProjection(Slice s)
        : base(Operator.Projection)
    {
        _slice = s;
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

        var start = _slice.GetStart(current.GetArrayLength());
        var end = _slice.GetStop(current.GetArrayLength());
        var step = _slice.Step;

        if (step == 0)
        {
            value = JsonConstants.Null;
            return false;
        }

        var result = new List<IValue>();
        if (step > 0)
        {
            if (start < 0)
            {
                start = 0;
            }
            if (end > current.GetArrayLength())
            {
                end = current.GetArrayLength();
            }
            for (var i = start; i < end; i += step)
            {
                if (!TryApplyExpressions(resources, current[i], out var val))
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
        else
        {
            if (start >= current.GetArrayLength())
            {
                start = current.GetArrayLength() - 1;
            }
            if (end < -1)
            {
                end = -1;
            }
            for (var i = start; i > end; i += step)
            {
                if (!TryApplyExpressions(resources, current[i], out var val))
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
        return "SliceProjection";
    }
}