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

using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Operators;

/// <summary>
/// Base class for all binary operators that are left associative.
/// </summary>
internal sealed class OrOperator : BinaryOperator
{
    /// <summary>
    /// Singleton instance of the <see cref="OrOperator"/> class.
    /// </summary>
    internal static OrOperator Instance { get; } = new();

    private OrOperator()
        : base(Operator.Or)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        if (lhs.Type == JmesPathType.Null && rhs.Type == JmesPathType.Null)
        {
            result = lhs;
            return true;
        }
        result = Expression.IsTrue(lhs) ? lhs : rhs;
        return true;
    }

    public override string ToString()
    {
        return "OrOperator";
    }
}