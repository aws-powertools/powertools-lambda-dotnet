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

namespace AWS.Lambda.Powertools.JMESPath.Values;

internal readonly struct DecimalValue : IValue
{
    private readonly decimal _value;

    internal DecimalValue(decimal value)
    {
        _value = value;
    }

    public JmesPathType Type => JmesPathType.Number;

    public IValue this[int index] => throw new InvalidOperationException();

    public int GetArrayLength()
    {
        throw new InvalidOperationException();
    }

    public string GetString()
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDecimal(out decimal value)
    {
        value = _value;
        return true;
    }

    public bool TryGetDouble(out double value)
    {
        value = (double)_value;
        return true;
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

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