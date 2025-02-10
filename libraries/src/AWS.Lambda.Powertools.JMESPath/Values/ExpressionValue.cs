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

using System;
using AWS.Lambda.Powertools.JMESPath.Expressions;

namespace AWS.Lambda.Powertools.JMESPath.Values;

/// <summary>
/// Represents a JMESPath expression.
/// </summary>
internal readonly struct ExpressionValue : IValue
{
    /// <summary>
    /// The expression to evaluate.
    /// </summary>
    private readonly IExpression _expr;

    internal ExpressionValue(IExpression expr)
    {
        _expr = expr;
    }

    /// <inheritdoc />
    public JmesPathType Type => JmesPathType.Expression;

    /// <inheritdoc />
    public IValue this[int index] => throw new InvalidOperationException();

    /// <inheritdoc />
    public int GetArrayLength()
    {
        throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public string GetString()
    {
        throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public bool TryGetDecimal(out decimal value)
    {
        throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public bool TryGetDouble(out double value)
    {
        throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public IExpression GetExpression()
    {
        return _expr;
    }

    public override string ToString()
    {
        return "expression";
    }
}