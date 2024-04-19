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
using System.Collections.Generic;
using System.Text;

namespace AWS.Lambda.Powertools.JMESPath.Values;

internal readonly struct ArrayValue : IValue
{
    private sealed class ArrayEnumerator : IArrayValueEnumerator
    {
        private readonly IList<IValue> _value;
        private readonly System.Collections.IEnumerator _enumerator;

        public ArrayEnumerator(IList<IValue> value)
        {
            _value = value;
            _enumerator = value.GetEnumerator();
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset() { _enumerator.Reset(); }

        void IDisposable.Dispose() {}

        public IValue Current => _enumerator.Current as IValue ?? throw new InvalidOperationException("Current cannot be null");

        object System.Collections.IEnumerator.Current => Current;

        public IEnumerator<IValue> GetEnumerator()
        {
            return _value.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private readonly IList<IValue> _value;

    internal ArrayValue(IList<IValue> value)
    {
        _value = value;
    }

    public JmesPathType Type => JmesPathType.Array;

    public IValue this[int index] => _value[index];

    public int GetArrayLength() { return _value.Count; }

    public string GetString()
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDecimal(out decimal value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDouble(out double value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        return new ArrayEnumerator(_value);
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
        var buffer = new StringBuilder();
        buffer.Append('[');
        var first = true;
        foreach (var item in _value)
        {
            if (!first)
            {
                buffer.Append(',');
            }
            else
            {
                first = false;
            }

            buffer.Append(item);
        }

        buffer.Append(']');
        return buffer.ToString();
    }
}