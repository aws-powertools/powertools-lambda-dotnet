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
/// Represents a function expression.
/// </summary>
internal sealed class FunctionExpression : BaseExpression
{
    /// <summary>
    /// The expression to evaluate.
    /// </summary>
    private readonly Expression _expr;

    internal FunctionExpression(Expression expr)
        : base(Operator.Default, false)
    {
        _expr = expr;
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources,
        IValue current, 
        out IValue value)
    {
        if (!_expr.TryEvaluate(resources, current, out var val))
        {
            value = JsonConstants.Null;
            return true;
        }
        value = val;
        return true;
    }

    public override string ToString()
    {
        return "FunctionExpression";
    }
}