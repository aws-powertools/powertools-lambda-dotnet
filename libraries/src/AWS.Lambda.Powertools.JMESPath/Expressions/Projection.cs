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
/// Base class for projection expressions.
/// </summary>
internal abstract class Projection : BaseExpression
{
    /// <summary>
    /// List of expressions to be applied to the current value.
    /// </summary>
    private readonly List<IExpression> _expressions;

    private protected Projection(Operator oper)
        : base(oper, true)
    {
        _expressions = new List<IExpression>();
    }

    /// <summary>
    /// Adds an expression to the list of expressions to be applied to the current value.
    /// </summary>
    /// <param name="expr"></param>
    public override void AddExpression(IExpression expr)
    {
        if (_expressions.Count != 0 && _expressions[_expressions.Count-1].IsProjection && 
            (expr.PrecedenceLevel > _expressions[_expressions.Count-1].PrecedenceLevel ||
             (expr.PrecedenceLevel == _expressions[_expressions.Count-1].PrecedenceLevel && expr.IsRightAssociative)))
        {
            _expressions[_expressions.Count-1].AddExpression(expr);
        }
        else
        {
            _expressions.Add(expr);
        }
    }
    
    /// <summary>
    /// Tries to apply the list of expressions to the current value.
    /// </summary>
    /// <param name="resources"></param>
    /// <param name="current"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    internal bool TryApplyExpressions(DynamicResources resources, IValue current, out IValue value)
    {
        value = current;
        foreach (var expression in _expressions)
        {
            if (!expression.TryEvaluate(resources, value, out value))
            {
                return false;
            }
        }
        return true;
    }
}