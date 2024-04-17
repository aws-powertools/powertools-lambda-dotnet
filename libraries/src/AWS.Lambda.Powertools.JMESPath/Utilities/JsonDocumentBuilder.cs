using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AWS.Lambda.Powertools.JMESPath.Utilities
{
    internal class JsonDocumentBuilder
    {
        internal static JsonDocumentBuilder Default { get; } = new();

        internal JsonValueKind ValueKind {get;}

        private object? _item;

        private IList<JsonDocumentBuilder> GetList()
        {
            return _item as IList<JsonDocumentBuilder> ?? throw new InvalidOperationException("Item is null");
        }

        private IDictionary<string,JsonDocumentBuilder> GetDictionary()
        {
            return _item as IDictionary<string, JsonDocumentBuilder> ?? throw new InvalidOperationException("Item is null");
        }
        internal JsonDocumentBuilder()
            : this(JsonValueKind.Null)
        {
        }

        internal JsonDocumentBuilder(JsonValueKind valueKind)
        {
            ValueKind = valueKind;
            switch (valueKind)
            {
                case JsonValueKind.Array:
                    _item = new List<JsonDocumentBuilder>();
                    break;
                case JsonValueKind.Object:
                    _item = new Dictionary<string,JsonDocumentBuilder>();
                    break;
                case JsonValueKind.True:
                    _item = true;
                    break;
                case JsonValueKind.False:
                    _item = false;
                    break;
                case JsonValueKind.Null:
                    _item = null;
                    break;
                case JsonValueKind.String:
                    _item = "";
                    break;
                case JsonValueKind.Number:
                    _item = 0;
                    break;
                default:
                    _item = null;
                    break;
            }
        }

        internal JsonDocumentBuilder(IList<JsonDocumentBuilder> list)
        {
            ValueKind = JsonValueKind.Array;
            _item = list;
        }

        internal JsonDocumentBuilder(IDictionary<string,JsonDocumentBuilder> dict)
        {
            ValueKind = JsonValueKind.Object;
            _item = dict;
        }

        internal JsonDocumentBuilder(string str)
        {
            ValueKind = JsonValueKind.String;
            _item = str;
        }

        internal JsonDocumentBuilder(JsonElement element)
        {
            ValueKind = element.ValueKind;
            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                    var list = new List<JsonDocumentBuilder>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(new JsonDocumentBuilder(item));
                    }
                    _item = list;
                    break;
                case JsonValueKind.Object:
                    var dict = new Dictionary<string,JsonDocumentBuilder>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dict.Add(property.Name, new JsonDocumentBuilder(property.Value));
                    }
                    _item = dict;
                    break;
                default:
                    _item = element;
                    break;
            }
        }

        internal IEnumerable<JsonDocumentBuilder> EnumerateArray()
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            return GetList();
        }

        internal IEnumerable<KeyValuePair<string, JsonDocumentBuilder>> EnumerateObject()
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return GetDictionary();
        }

        internal JsonDocumentBuilder this[int i]
        {
            get { 
                if (ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("This value's ValueKind is not Array.");
                }
                return GetList() [i]; 
            }
            set { 
                if (ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("This value's ValueKind is not Array.");
                }
                GetList()[i] = value; 
            }
        }

        internal void AddArrayItem(JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            GetList().Add(value);
        }

        internal void InsertArrayItem(int index, JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            GetList().Insert(index, value);
        }

        internal void RemoveArrayItemAt(int index)
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            GetList().RemoveAt(index);
        }

        internal void AddProperty(string name, JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            GetDictionary().Add(name, value);
        }

        internal bool TryAddProperty(string name, JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return GetDictionary().TryAdd(name, value);
        }

        internal bool ContainsPropertyName(string name)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return GetDictionary().ContainsKey(name);
        }

        internal void RemoveProperty(string name)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            GetDictionary().Remove(name);
        }

        internal int GetArrayLength()
        {
            if (ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("This value's ValueKind is not Array.");
            }
            return GetList().Count();
        }

        internal int GetObjectLength()
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            return GetDictionary().Count();
        }

        internal bool TryGetProperty(string name, out JsonDocumentBuilder value)
        {
            if (ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("This value's ValueKind is not Object.");
            }
            if (ValueKind != JsonValueKind.Object)
            {
                value = Default;
                return false;
            }
            return GetDictionary().TryGetValue(name, out value);
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            ToString(buffer);
            return buffer.ToString();
        }

        private void ToString(StringBuilder buffer)
        {
            switch (ValueKind)
            {
                case JsonValueKind.Array:
                {
                    buffer.Append("[");
                    var first = true;
                    foreach (var item in EnumerateArray())
                    {
                        if (!first)
                        {
                            buffer.Append(",");
                        }
                        else
                        {
                            first = false;
                        }
                        item.ToString(buffer);
                    }
                    buffer.Append("]");
                    break;
                }
                case JsonValueKind.Object:
                {
                    buffer.Append("{");
                    var first = true;
                    foreach (var property in EnumerateObject())
                    {
                        if (!first)
                        {
                            buffer.Append(",");
                        }
                        else
                        {
                            first = false;
                        }
                        buffer.Append(JsonSerializer.Serialize(property.Key));
                        buffer.Append(":");
                        property.Value.ToString(buffer);
                    }
                    buffer.Append("}");
                    break;
                }
                default:
                {
                    buffer.Append(JsonSerializer.Serialize(_item, (JsonSerializerOptions)null));
                    break;
                }
            }
        }

        internal JsonDocument ToJsonDocument()
        {
            var json = ToString();
            return JsonDocument.Parse(json);
        }
    }

} // namespace JsonCons.Utilities
