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
/// Represents a multi-select hash expression.
/// </summary>
internal sealed class MultiSelectHash : BaseExpression
{
    /// <summary>
    /// The list of key expression pairs.
    /// </summary>
    private readonly IList<KeyExpressionPair> _keyExprPairs;

    internal MultiSelectHash(IList<KeyExpressionPair> keyExprPairs)
        : base(Operator.Default, false)
    {
        _keyExprPairs = keyExprPairs;
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources,
        IValue current, 
        out IValue value)
    {
        if (current.Type == JmesPathType.Null)
        {
            value = JsonConstants.Null;
            return true;
        }
        var result = new Dictionary<string,IValue>();
        foreach (var item in _keyExprPairs)
        {
            if (!item.Expression.TryEvaluate(resources, current, out var val))
            {
                value = JsonConstants.Null;
                return false;
            }
            result.Add(item.Key, val);
        }

        value = new ObjectValue(result);
        return true;
    }

    public override string ToString()
    {
        return "MultiSelectHash";
    }
}