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
using System.Collections.Generic;
using System.Text;
using AWS.Lambda.Powertools.JMESPath.Expressions;

namespace AWS.Lambda.Powertools.JMESPath.Values;

/// <summary>
/// Represents a JMESPath array value.
/// </summary>
internal readonly struct ArrayValue : IValue
{
    /// <summary>
    /// An enumerator for a JMESPath array value.
    /// </summary>
    private sealed class ArrayEnumerator : IArrayValueEnumerator
    {
        /// <summary>
        /// The list of values in the array.
        /// </summary>
        private readonly IList<IValue> _value;
        /// <summary>
        /// The enumerator for the list of values in the array.
        /// </summary>
        private readonly System.Collections.IEnumerator _enumerator;

        public ArrayEnumerator(IList<IValue> value)
        {
            _value = value;
            _enumerator = value.GetEnumerator();
        }

        /// <summary>
        /// Gets the current value in the array.
        /// </summary>
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <summary>
        /// Resets the enumerator to the beginning of the array.
        /// </summary>
        public void Reset() { _enumerator.Reset(); }

        void IDisposable.Dispose() {}

        /// <summary>
        /// Gets the current value in the array.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public IValue Current => _enumerator.Current as IValue ?? throw new InvalidOperationException("Current cannot be null");

        /// <summary>
        /// Gets the current value in the array.
        /// </summary>
        object System.Collections.IEnumerator.Current => Current;

        /// <summary>
        /// Gets an enumerator for the array.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IValue> GetEnumerator()
        {
            return _value.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator for the array.
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// The list of values in the array.
    /// </summary>
    private readonly IList<IValue> _value;

    /// <summary>
    /// Creates a new array value.
    /// </summary>
    internal ArrayValue(IList<IValue> value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the type of the value.
    /// </summary>
    public JmesPathType Type => JmesPathType.Array;

    /// <summary>
    /// Gets the value at the specified index.
    /// </summary>
    public IValue this[int index] => _value[index];

    /// <summary>
    /// Gets the length of the array.
    /// </summary>
    /// <returns></returns>
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