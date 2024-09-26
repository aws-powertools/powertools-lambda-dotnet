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
using AWS.Lambda.Powertools.JMESPath.Serializers;

namespace AWS.Lambda.Powertools.JMESPath.Values;

/// <summary>
/// Represents a JMESPath number value.
/// </summary>
internal readonly struct DecimalValue : IValue
{
    /// <summary>
    /// The value of the JMESPath number.
    /// </summary>
    private readonly decimal _value;

    internal DecimalValue(decimal value)
    {
        _value = value;
    }

    /// <summary>
    /// The type of the JMESPath value.
    /// </summary>
    public JmesPathType Type => JmesPathType.Number;

    /// <summary>
    /// Gets the value at the specified index.
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="InvalidOperationException"></exception>
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
        value = _value;
        return true;
    }

    /// <inheritdoc />
    public bool TryGetDouble(out double value)
    {
        value = (double)_value;
        return true;
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
        throw new InvalidOperationException("Not an expression");
    }

    public override string ToString()
    {
        return JMESPathSerializer.Serialize(_value, typeof(decimal));
    }
}