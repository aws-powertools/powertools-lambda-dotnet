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
using AWS.Lambda.Powertools.JMESPath.Serializers;

namespace AWS.Lambda.Powertools.JMESPath.Values;

/// <summary>
/// Represents an object value.
/// </summary>
internal readonly struct ObjectValue : IValue
{
    /// <summary>
    /// An <see cref="IObjectValueEnumerator"/> that can be used to iterate over the
    /// </summary>
    private sealed class ObjectEnumerator : IObjectValueEnumerator
    {
        /// <summary>
        /// The underlying <see cref="IDictionary{TKey, TValue}"/> that is being enumerated.
        /// </summary>
        private readonly IDictionary<string, IValue> _value;
        
        /// <summary>
        /// The underlying <see cref="System.Collections.IEnumerator"/> that is being enumerated.
        /// </summary>
        private readonly System.Collections.IEnumerator _enumerator;

        public ObjectEnumerator(IDictionary<string, IValue> value)
        {
            _value = value;
            using var enumerator = value.GetEnumerator();
            _enumerator = enumerator;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        void IDisposable.Dispose()
        {
        }

        /// <inheritdoc />
        public NameValuePair Current
        {
            get
            {
                var pair = (KeyValuePair<string, IValue>)_enumerator.Current!;
                return new NameValuePair(pair.Key, pair.Value);
            }
        }

        /// <inheritdoc />
        object System.Collections.IEnumerator.Current => Current;

        public IEnumerator<NameValuePair> GetEnumerator()
        {
            return new ObjectEnumerator(_value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private readonly IDictionary<string, IValue> _value;

    internal ObjectValue(IDictionary<string, IValue> value)
    {
        _value = value;
    }

    /// <inheritdoc />
    public JmesPathType Type => JmesPathType.Object;

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
        return _value.TryGetValue(propertyName, out property);
    }

    /// <inheritdoc />
    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public IObjectValueEnumerator EnumerateObject()
    {
        return new ObjectEnumerator(_value);
    }

    /// <inheritdoc />
    public IExpression GetExpression()
    {
        throw new InvalidOperationException("Not an expression");
    }

    public override string ToString()
    {
        var buffer = new StringBuilder();
        buffer.Append('{');
        var first = true;
        foreach (var property in _value)
        {
            if (!first)
            {
                buffer.Append(',');
            }
            else
            {
                first = false;
            }

            buffer.Append(JmesPathSerializer.Serialize(property.Key, property.Key.GetType()));
            buffer.Append(':');
            buffer.Append(property.Value);
        }

        buffer.Append('}');
        return buffer.ToString();
    }
}