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

using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Expressions;

/// <summary>
/// Represents a single index selector.
/// </summary>
internal sealed class IndexSelector : BaseExpression
{
    /// <summary>
    /// The index of the selector.
    /// </summary>
    private readonly int _index;
    internal IndexSelector(int index)
        : base(Operator.Default, false)
    {
        _index = index;
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
        var slen = current.GetArrayLength();
        if (_index >= 0 && _index < slen)
        {
            value = current[_index];
        }
        else if ((slen + _index) >= 0 && (slen+_index) < slen)
        {
            var index = slen + _index;
            value = current[index];
        }
        else
        {
            value = JsonConstants.Null;
        }
        return true;
    }

    public override string ToString()
    {
        return $"Index Selector {_index}";
    }
}