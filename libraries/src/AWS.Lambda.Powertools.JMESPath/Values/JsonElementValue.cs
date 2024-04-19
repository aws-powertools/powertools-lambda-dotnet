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
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AWS.Lambda.Powertools.JMESPath.Values;

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