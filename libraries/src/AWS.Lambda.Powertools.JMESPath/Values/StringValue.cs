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

using System;
using System.Text.Json;
using AWS.Lambda.Powertools.JMESPath.Expressions;

namespace AWS.Lambda.Powertools.JMESPath.Values;

/// <summary>
/// Represents a string value.
/// </summary>
internal readonly struct StringValue : IValue
{
    /// <summary>
    /// The string value.
    /// </summary>
    private readonly string _value;

    internal StringValue(string value)
    {
        _value = value;
    }

    /// <inheritdoc />
    public JmesPathType Type => JmesPathType.String;

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
        return _value;
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
        throw new InvalidOperationException("Not an expression");
    }

    public override string ToString()
    {
        var s = JsonSerializer.Serialize(_value);
        return s;
    }
}