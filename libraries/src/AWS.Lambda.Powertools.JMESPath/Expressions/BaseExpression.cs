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
/// Base class for all expressions.
/// </summary>
internal abstract class BaseExpression : IExpression
{
    /// <inheritdoc />
    public int PrecedenceLevel {get;} 

    /// <inheritdoc />
    public bool IsRightAssociative {get;} 

    /// <inheritdoc />
    public bool IsProjection {get;}

    private protected BaseExpression(Operator oper, bool isProjection)
    {
        PrecedenceLevel = OperatorTable.PrecedenceLevel(oper);
        IsRightAssociative = OperatorTable.IsRightAssociative(oper);
        IsProjection = isProjection;
    }

    /// <inheritdoc />
    public abstract bool TryEvaluate(DynamicResources resources,
        IValue current, 
        out IValue value);

    /// <inheritdoc />
    public virtual void AddExpression(IExpression expr)
    {
    }

    public override string ToString()
    {
        return "ToString not implemented";
    }
}