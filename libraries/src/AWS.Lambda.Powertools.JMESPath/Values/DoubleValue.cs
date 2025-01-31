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
using System.Text.Json;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Serializers;

namespace AWS.Lambda.Powertools.JMESPath.Values;

/// <summary>
/// Represents a double value.
/// </summary>
internal readonly struct DoubleValue : IValue
{
    /// <summary>
    /// The value of this <see cref="DoubleValue"/>.
    /// </summary>
    private readonly double _value;

    internal DoubleValue(double value)
    {
        _value = value;
    }

    /// <inheritdoc />
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
        if (!(double.IsNaN(_value) || double.IsInfinity(_value)) &&
            _value is >= (double)decimal.MinValue and <= (double)decimal.MaxValue)
        {
            value = decimal.MinValue;
            return false;
        }

        value = new decimal(_value);
        return true;
    }

    /// <inheritdoc />
    public bool TryGetDouble(out double value)
    {
        value = _value;
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
        return JmesPathSerializer.Serialize(_value, typeof(double));
    }
}