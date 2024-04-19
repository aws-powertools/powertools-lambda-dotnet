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
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AWS.Lambda.Powertools.JMESPath
{
    internal readonly struct NameValuePair
    {
        public string Name { get; }
        public IValue Value { get; }

        public NameValuePair(string name, IValue value)
        {
            Name = name;
            Value = value;
        }
    }

    internal interface IArrayValueEnumerator : IEnumerator<IValue>, IEnumerable<IValue>
    {
    }

    internal interface IObjectValueEnumerator : IEnumerator<NameValuePair>, IEnumerable<NameValuePair>
    {
    }

    internal enum JmesPathType
    {
        Null,
        Array,
        False,
        Number,
        Object,
        String,
        True,
        Expression
    }

    internal interface IValue
    {
        JmesPathType Type { get; }
        IValue this[int index] { get; }
        int GetArrayLength();
        string GetString();
        bool TryGetDecimal(out decimal value);
        bool TryGetDouble(out double value);
        bool TryGetProperty(string propertyName, out IValue property);
        IArrayValueEnumerator EnumerateArray();
        IObjectValueEnumerator EnumerateObject();
        IExpression GetExpression();
    }

    internal readonly struct JsonElementValue : IValue
    {
        private class ArrayEnumerator : IArrayValueEnumerator
        {
            private JsonElement.ArrayEnumerator _enumerator;

            public ArrayEnumerator(JsonElement.ArrayEnumerator enumerator)
            {
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

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // Cleanup
                if (disposing)
                {
                    _enumerator.Dispose();
                }
            }

            public IValue Current => new JsonElementValue(_enumerator.Current);

            object System.Collections.IEnumerator.Current => Current;

            public IEnumerator<IValue> GetEnumerator()
            {
                return new ArrayEnumerator(_enumerator.GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ObjectEnumerator : IObjectValueEnumerator
        {
            private JsonElement.ObjectEnumerator _enumerator;

            public ObjectEnumerator(JsonElement.ObjectEnumerator enumerator)
            {
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

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // Cleanup
                if (disposing)
                {
                    _enumerator.Dispose();
                }
            }

            public NameValuePair Current =>
                new(_enumerator.Current.Name, new JsonElementValue(_enumerator.Current.Value));

            object System.Collections.IEnumerator.Current => Current;

            public IEnumerator<NameValuePair> GetEnumerator()
            {
                return new ObjectEnumerator(_enumerator);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private readonly JsonElement _element;

        internal JsonElementValue(JsonElement element)
        {
            _element = element;
        }

        public JmesPathType Type
        {
            get
            {
                switch (_element.ValueKind)
                {
                    case JsonValueKind.Array:
                        return JmesPathType.Array;
                    case JsonValueKind.False:
                        return JmesPathType.False;
                    case JsonValueKind.Number:
                        return JmesPathType.Number;
                    case JsonValueKind.Object:
                        return JmesPathType.Object;
                    case JsonValueKind.String:
                        return JmesPathType.String;
                    case JsonValueKind.True:
                        return JmesPathType.True;
                    default:
                        return JmesPathType.Null;
                }
            }
        }

        public IValue this[int index] => new JsonElementValue(_element[index]);

        public int GetArrayLength()
        {
            return _element.GetArrayLength();
        }

        public string GetString()
        {
            return _element.GetString() ?? throw new InvalidOperationException("String cannot be null");
        }

        public bool TryGetDecimal(out decimal value)
        {
            return _element.TryGetDecimal(out value);
        }

        public bool TryGetDouble(out double value)
        {
            return _element.TryGetDouble(out value);
        }

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            var r = _element.TryGetProperty(propertyName, out var prop);

            property = prop.ValueKind == JsonValueKind.String && IsJsonValid(prop.GetString())
                ? new JsonElementValue(JsonNode.Parse(prop.GetString() ?? string.Empty).Deserialize<JsonElement>())
                : new JsonElementValue(prop);

            return r;
        }

        private static bool IsJsonValid(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using var jsonDoc = JsonDocument.Parse(json);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            return new ArrayEnumerator(_element.EnumerateArray());
        }

        public IObjectValueEnumerator EnumerateObject()
        {
            return new ObjectEnumerator(_element.EnumerateObject());
        }

        public IExpression GetExpression()
        {
            throw new InvalidOperationException("Not an expression");
        }

        public override string ToString()
        {
            var s = JsonSerializer.Serialize(_element);
            return s;
        }
    }

    internal readonly struct DoubleValue : IValue
    {
        private readonly double _value;

        internal DoubleValue(double value)
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
            if (!(double.IsNaN(_value) || double.IsInfinity(_value)) &&
                _value is >= (double)decimal.MinValue and <= (double)decimal.MaxValue)
            {
                value = decimal.MinValue;
                return false;
            }

            value = new decimal(_value);
            return true;
        }

        public bool TryGetDouble(out double value)
        {
            value = _value;
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

    internal readonly struct StringValue : IValue
    {
        private readonly string _value;

        internal StringValue(string value)
        {
            _value = value;
        }

        public JmesPathType Type => JmesPathType.String;

        public IValue this[int index] => throw new InvalidOperationException();

        public int GetArrayLength()
        {
            throw new InvalidOperationException();
        }

        public string GetString()
        {
            return _value;
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

    internal readonly struct TrueValue : IValue
    {
        public JmesPathType Type => JmesPathType.True;

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
            return "true";
        }
    }

    internal readonly struct FalseValue : IValue
    {
        public JmesPathType Type => JmesPathType.False;

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
            return "false";
        }
    }

    internal readonly struct NullValue : IValue
    {
        public JmesPathType Type => JmesPathType.Null;

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
            return "null";
        }
    }

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

    internal readonly struct ObjectValue : IValue
    {
        private sealed class ObjectEnumerator : IObjectValueEnumerator
        {
            private readonly IDictionary<string, IValue> _value;
            private readonly System.Collections.IEnumerator _enumerator;

            public ObjectEnumerator(IDictionary<string, IValue> value)
            {
                _value = value;
                _enumerator = value.GetEnumerator();
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

            public NameValuePair Current
            {
                get
                {
                    var pair = (KeyValuePair<string, IValue>)_enumerator.Current!;
                    return new NameValuePair(pair.Key, pair.Value);
                }
            }

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

        public JmesPathType Type => JmesPathType.Object;

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
            throw new InvalidOperationException();
        }

        public bool TryGetDouble(out double value)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetProperty(string propertyName, out IValue property)
        {
            return _value.TryGetValue(propertyName, out property);
        }

        public IArrayValueEnumerator EnumerateArray()
        {
            throw new InvalidOperationException();
        }

        public IObjectValueEnumerator EnumerateObject()
        {
            return new ObjectEnumerator(_value);
        }

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

                buffer.Append(JsonSerializer.Serialize(property.Key));
                buffer.Append(':');
                buffer.Append(property.Value);
            }

            buffer.Append('}');
            return buffer.ToString();
        }
    }

    internal readonly struct ExpressionValue : IValue
    {
        private readonly IExpression _expr;

        internal ExpressionValue(IExpression expr)
        {
            _expr = expr;
        }

        public JmesPathType Type => JmesPathType.Expression;

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
            throw new InvalidOperationException();
        }

        public IObjectValueEnumerator EnumerateObject()
        {
            throw new InvalidOperationException();
        }

        public IExpression GetExpression()
        {
            return _expr;
        }

        public override string ToString()
        {
            return "expression";
        }
    }
}