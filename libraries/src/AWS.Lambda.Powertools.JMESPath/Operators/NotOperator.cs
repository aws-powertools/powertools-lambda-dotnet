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
/// Represents the not operator.
/// </summary>
internal sealed class NotOperator : UnaryOperator
{
    /// <summary>
    /// The singleton instance of the <see cref="NotOperator"/> class.
    /// </summary>
    internal static NotOperator Instance { get; } = new();

    private NotOperator()
        : base(Operator.Not)
    {}

    /// <inheritdoc />
    public override bool TryEvaluate(IValue elem, out IValue result)
    {
        result = Expression.IsFalse(elem) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "Not";
    }
}