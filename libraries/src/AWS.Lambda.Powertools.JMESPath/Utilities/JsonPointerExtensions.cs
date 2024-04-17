using System.Collections.Generic;
using System.Text.Json;

namespace AWS.Lambda.Powertools.JMESPath.Utilities
{
    internal static class JsonPointerExtensions
    {
        public static bool TryResolve(string token, JsonDocumentBuilder current, out JsonDocumentBuilder result)
        {
            result = current;

            if (result.ValueKind == JsonValueKind.Array)
            {
                if (token == "-")
                {
                    return false;
                }

                if (!int.TryParse(token, out var index))
                {
                    return false;
                }
                if (index >= result.GetArrayLength())
                {
                    return false;
                }
                result = result[index];
            }
            else if (result.ValueKind == JsonValueKind.Object)
            {
                if (!result.TryGetProperty(token, out result))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public static JsonPointer ToDefinitePath(this JsonPointer pointer, JsonDocumentBuilder value)
        {
            if (value.ValueKind == JsonValueKind.Array && pointer.Tokens.Count > 0 && pointer.Tokens[pointer.Tokens.Count-1] == "-")
            {
                var tokens = new List<string>();
                for (var i = 0; i < pointer.Tokens.Count-1; ++i)
                {
                    tokens.Add(pointer.Tokens[i]);
                }
                tokens.Add(value.GetArrayLength().ToString());
                return new JsonPointer(tokens);
            }
            else
            {
                return pointer;
            }
        }

        public static bool TryGetValue(this JsonPointer pointer, JsonDocumentBuilder root, out JsonDocumentBuilder value)
        {
            value = root;

            foreach (var token in pointer)
            {
                if (!TryResolve(token,value,out value))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryAdd(this JsonPointer location, 
                                  ref JsonDocumentBuilder root, 
                                  JsonDocumentBuilder value)
        {
            var current = root;
            var token = "";

            var enumerator = location.GetEnumerator();
            var more = enumerator.MoveNext();
            if (!more)
            {
                return false;
            }
            while (more)
            {
                token = enumerator.Current;
                more = enumerator.MoveNext();
                if (more)
                {
                    if (!TryResolve(token, current, out current))
                    {
                        return false;
                    }
                }
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                if (token.Length == 1 && token[0] == '-')
                {
                    current.AddArrayItem(value);
                    current = current[current.GetArrayLength()-1];
                }
                else
                {
                    if (!int.TryParse(token, out var index))
                    {
                        return false;
                    }
                    if (index > current.GetArrayLength())
                    {
                        return false;
                    }
                    if (index == current.GetArrayLength())
                    {
                        current.AddArrayItem(value);
                        current = value;
                    }
                    else
                    {
                        current.InsertArrayItem(index,value);
                        current = value;
                    }
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.ContainsPropertyName(token))
                {
                    current.RemoveProperty(token);
                }
                current.AddProperty(token, value);
                current = value;
            }
            else
            {
                return false;
            }
            return true;
        }

        public static bool TryAddIfAbsent(this JsonPointer location, 
                                          ref JsonDocumentBuilder root, 
                                          JsonDocumentBuilder value)
        {
            var current = root;
            var token = "";

            var enumerator = location.GetEnumerator();
            var more = enumerator.MoveNext();
            if (!more)
            {
                return false;
            }
            while (more)
            {
                token = enumerator.Current;
                more = enumerator.MoveNext();
                if (more)
                {
                    if (!TryResolve(token, current, out current))
                    {
                        return false;
                    }
                }
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                if (token.Length == 1 && token[0] == '-')
                {
                    current.AddArrayItem(value);
                    current = current[current.GetArrayLength()-1];
                }
                else
                {
                    if (!int.TryParse(token, out var index))
                    {
                        return false;
                    }
                    if (index > current.GetArrayLength())
                    {
                        return false;
                    }
                    if (index == current.GetArrayLength())
                    {
                        current.AddArrayItem(value);
                        current = value;
                    }
                    else
                    {
                        current.InsertArrayItem(index,value);
                        current = value;
                    }
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.ContainsPropertyName(token))
                {
                    return false;
                }
                current.AddProperty(token, value);
                current = value;
            }
            else
            {
                return false;
            }
            return true;
        }

        public static bool TryRemove(this JsonPointer location, 
                                     ref JsonDocumentBuilder root)
        {
            var current = root;
            var token = "";

            var enumerator = location.GetEnumerator();
            var more = enumerator.MoveNext();
            if (!more)
            {
                return false;
            }
            while (more)
            {
                token = enumerator.Current;
                more = enumerator.MoveNext();
                if (more)
                {
                    if (!TryResolve(token, current, out current))
                    {
                        return false;
                    }
                }
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                if (token.Length == 1 && token[0] == '-')
                {
                    return false;
                }
                else
                {
                    if (!int.TryParse(token, out var index))
                    {
                        return false;
                    }
                    if (index >= current.GetArrayLength())
                    {
                        return false;
                    }
                    current.RemoveArrayItemAt(index);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.ContainsPropertyName(token))
                {
                    current.RemoveProperty(token);
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        public static bool TryReplace(this JsonPointer location, 
                                      ref JsonDocumentBuilder root, 
                                      JsonDocumentBuilder value)
        {
            var current = root;
            var token = "";

            var enumerator = location.GetEnumerator();
            var more = enumerator.MoveNext();
            if (!more)
            {
                return false;
            }
            while (more)
            {
                token = enumerator.Current;
                more = enumerator.MoveNext();
                if (more)
                {
                    if (!TryResolve(token, current, out current))
                    {
                        return false;
                    }
                }
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                if (token.Length == 1 && token[0] == '-')
                {
                    return false;
                }
                else
                {
                    if (!int.TryParse(token, out var index))
                    {
                        return false;
                    }
                    if (index >= current.GetArrayLength())
                    {
                        return false;
                    }
                    current[index] = value;
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.ContainsPropertyName(token))
                {
                    current.RemoveProperty(token);
                }
                else
                {
                    return false;
                }
                current.AddProperty(token, value);
            }
            else
            {
                return false;
            }
            return true;
        }

    }

} // namespace JsonCons.Utilities
