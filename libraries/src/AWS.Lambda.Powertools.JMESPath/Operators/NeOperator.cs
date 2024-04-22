/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Operators;

/// <summary>
/// Base class for all binary operators that are inequality
/// </summary>
internal sealed class NeOperator : BinaryOperator
{
    /// <summary>
    /// Singleton instance of the <see cref="NeOperator"/> class
    /// </summary>
    internal static NeOperator Instance { get; } = new();

    private NeOperator()
        : base(Operator.Ne)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
    {
        if (!EqOperator.Instance.TryEvaluate(lhs, rhs, out var value))
        {
            result = JsonConstants.Null;
            return false;
        }
                
        result = Expression.IsFalse(value) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "NeOperator";
    }
}