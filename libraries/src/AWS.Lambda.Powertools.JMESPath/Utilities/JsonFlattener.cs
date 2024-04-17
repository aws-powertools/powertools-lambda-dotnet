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
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AWS.Lambda.Powertools.JMESPath.Utilities
{
    /// <summary>
    /// Defines how the unflatten operation handles integer tokens in a JSON Pointer 
    /// </summary>
    /// <example>
    /// This example illustrates the use of <see cref="IntegerTokenUnflattening"/>
    /// <code>
    /// using System;
    /// using System.Diagnostics;
    /// using System.Text.Json;
    /// 
    /// public class Example
    /// {
    ///    public static void Main()
    ///    {
    ///        using var doc = JsonDocument.Parse(@"
    /// {
    ///     ""discards"": {
    ///         ""1000"": ""Record does not exist"",
    ///         ""1004"": ""Queue limit exceeded"",
    ///         ""1010"": ""Discarding timed-out partial msg""
    ///     },
    ///     ""warnings"": {
    ///         ""0"": ""Phone number missing country code"",
    ///         ""1"": ""State code missing"",
    ///         ""2"": ""Zip code missing""
    ///     }
    /// }
    ///        ");
    ///    
    ///         var options = new JsonSerializerOptions() { WriteIndented = true };
    ///         
    ///         using JsonDocument flattened = JsonFlattener.Flatten(doc.RootElement);
    ///         Console.WriteLine("The flattened document:\n");
    ///         Console.WriteLine($"{JsonSerializer.Serialize(flattened, options)}\n");
    ///         
    ///         Console.WriteLine("Unflatten integer tokens as array indices if possible:\n");
    ///         using JsonDocument unflattened1 = JsonFlattener.Unflatten(flattened.RootElement,
    ///                                                             IntegerTokenUnflattening.TryIndex);
    ///         Console.WriteLine($"{JsonSerializer.Serialize(unflattened1, options)}\n");
    ///         
    ///         Console.WriteLine("Always unflatten integer tokens as object names:\n");
    ///         using JsonDocument unflattened2 = JsonFlattener.Unflatten(flattened.RootElement,
    ///                                                             IntegerTokenUnflattening.AssumeName);
    ///         Console.WriteLine($"{JsonSerializer.Serialize(unflattened2, options)}\n");
    ///     }
    /// }
    /// </code>
    /// Output:
    /// <code>
    /// The flattened document:
    /// 
    /// {
    ///   "/discards/1000": "Record does not exist",
    ///   "/discards/1004": "Queue limit exceeded",
    ///   "/discards/1010": "Discarding timed-out partial msg",
    ///   "/warnings/0": "Phone number missing country code",
    ///   "/warnings/1": "State code missing",
    ///   "/warnings/2": "Zip code missing"
    /// }
    /// 
    /// Unflatten integer tokens as array indices if possible:
    /// 
    /// {
    ///   "discards": {
    ///     "1000": "Record does not exist",
    ///     "1004": "Queue limit exceeded",
    ///     "1010": "Discarding timed-out partial msg"
    ///   },
    ///   "warnings": [
    ///     "Phone number missing country code",
    ///     "State code missing",
    ///     "Zip code missing"
    ///   ]
    /// }
    /// 
    /// Always unflatten integer tokens as object names:
    /// 
    /// {
    ///   "discards": {
    ///     "1000": "Record does not exist",
    ///     "1004": "Queue limit exceeded",
    ///     "1010": "Discarding timed-out partial msg"
    ///   },
    ///   "warnings": {
    ///     "0": "Phone number missing country code",
    ///     "1": "State code missing",
    ///     "2": "Zip code missing"
    ///   }
    /// }
    /// </code>
    /// </example>
    public enum IntegerTokenUnflattening { 
        /// <summary>
        /// The unflatten operation first tries to unflatten into a JSON array
        /// using the integer tokens as sequential indices, and if that fails, unflattens into
        /// a JSON object using the integer tokens as names.
        /// </summary>
        TryIndex, 
        /// <summary>
        /// The unflatten operation always unflattens into a JSON object
        /// using the integer tokens as names.
        /// </summary>
        AssumeName 
    }

    /// <summary>
    /// Provides functionality to flatten a JSON object or array to a single depth JSON object of JSON Pointer-value pairs,
    /// and to unflatten a flattened JSON object.
    /// </summary>
    /// <example>
    /// This example shows how to flatten and unflatten a JSON value
    /// <code>
    /// using System;
    /// using System.Diagnostics;
    /// using System.Text.Json;
    /// 
    /// public class Example
    /// {
    ///    public static void Main()
    ///    {
    ///        using var doc = JsonDocument.Parse(@"
    ///        {
    ///           ""application"": ""hiking"",
    ///           ""reputons"": [
    ///               {
    ///                   ""rater"": ""HikingAsylum"",
    ///                   ""assertion"": ""advanced"",
    ///                   ""rated"": ""Marilyn C"",
    ///                   ""rating"": 0.90
    ///                },
    ///                {
    ///                   ""rater"": ""HikingAsylum"",
    ///                   ""assertion"": ""intermediate"",
    ///                   ""rated"": ""Hongmin"",
    ///                   ""rating"": 0.75
    ///                }    
    ///            ]
    ///        }
    ///        ");
    ///    
    ///        using JsonDocument flattened = JsonFlattener.Flatten(doc.RootElement);
    ///    
    ///        var options = new JsonSerializerOptions() { WriteIndented = true };
    ///    
    ///        Console.WriteLine($"{JsonSerializer.Serialize(flattened, options)}\n");
    ///    
    ///        using JsonDocument unflattened = JsonFlattener.Unflatten(flattened.RootElement);
    ///    
    ///        var comparer = JsonElementEqualityComparer.Instance;
    ///        Debug.Assert(comparer.Equals(unflattened.RootElement,doc.RootElement));
    ///     }
    /// }
    /// </code>
    /// Output:
    /// <code>
    /// {
    ///   "/application": "hiking",
    ///   "/reputons/0/rater": "HikingAsylum",
    ///   "/reputons/0/assertion": "advanced",
    ///   "/reputons/0/rated": "Marilyn C",
    ///   "/reputons/0/rating": 0.90,
    ///   "/reputons/1/rater": "HikingAsylum",
    ///   "/reputons/1/assertion": "intermediate",
    ///   "/reputons/1/rated": "Hongmin",
    ///   "/reputons/1/rating": 0.75
    /// }
    /// </code>
    /// </example>

    public static class JsonFlattener
    {
        /// <summary>
        /// Converts a JSON object or array into a single depth JSON object of name-value pairs,
        /// such that the names are JSON Pointer strings, and the values are either string,
        /// number, true, false, null, empty object, or empty array. 
        /// </summary>
        /// <remarks>
        /// It is the users responsibility to properly Dispose the returned <see cref="JsonDocument"/> value
        /// </remarks>
        /// <param name="value">The value to be flattened.</param>
        /// <returns>The flattened value</returns>
        public static JsonDocument Flatten(JsonElement value)
        {
            var result = new JsonDocumentBuilder(JsonValueKind.Object);
            const string parentKey = "";
            _Flatten(parentKey, value, result);
            return result.ToJsonDocument();
        }

        /// <summary>
        /// Recovers the original JSON value from a JSON object in flattened form, to the extent possible. 
        /// There may not be a unique solution, an integer token in a JSON Pointer could be an array index or 
        /// it could be an object name. The default behavior is to attempt to recover arrays. The <paramref name="options"/>
        /// parameter can be used to recover objects with integer names instead.
        /// </summary>
        /// <remarks>
        /// It is the users responsibility to properly Dispose the returned <see cref="JsonDocument"/> value
        /// </remarks>
        /// <param name="flattenedValue">The flattened value, which must be a JSON object of name-value pairs, such that 
        /// the names are JSON Pointer strings, and the values are either string,
        /// number, true, false, null, empty object, or empty array.</param>
        /// <param name="options">Options for handling integer tokens in the JSON Pointer.</param>
        /// <returns>The unflattened value</returns>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="flattenedValue"/> is not a JSON object, or has a name that contains an invalid JSON pointer.
        /// </exception>
        public static JsonDocument Unflatten(JsonElement flattenedValue, 
                                             IntegerTokenUnflattening options = IntegerTokenUnflattening.TryIndex)
        {
            if (options == IntegerTokenUnflattening.TryIndex)
            {
                return TryUnflattenArray(flattenedValue, out var val) ? val.ToJsonDocument() : UnflattenToObject(flattenedValue, options).ToJsonDocument();
            }

            return UnflattenToObject(flattenedValue, options).ToJsonDocument();
        }

        private static void _Flatten(string parentKey,
                             JsonElement parentValue,
                             JsonDocumentBuilder result)
        {
            switch (parentValue.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    if (parentValue.GetArrayLength() == 0)
                    {
                        result.AddProperty(parentKey, new JsonDocumentBuilder(parentValue));
                    }
                    else
                    {
                        for (var i = 0; i < parentValue.GetArrayLength(); ++i)
                        {
                            var buffer = new StringBuilder(parentKey);
                            buffer.Append('/');
                            buffer.Append(i.ToString());
                            _Flatten(buffer.ToString(), parentValue[i], result);
                        }
                    }
                    break;
                }

                case JsonValueKind.Object:
                {
                    if (!parentValue.EnumerateObject().Any())
                    {
                        result.AddProperty(parentKey, new JsonDocumentBuilder(parentValue));
                    }
                    else
                    {
                        foreach (var item in parentValue.EnumerateObject())
                        {
                            var buffer = new StringBuilder(parentKey);
                            buffer.Append('/');
                            buffer.Append(JsonPointer.Escape(item.Name));
                            _Flatten(buffer.ToString(), item.Value, result);
                        }
                    }
                    break;
                }

                default:
                {
                    result.AddProperty(parentKey, new JsonDocumentBuilder(parentValue));
                    break;
                }
            }
        }

        // unflatten

        private static JsonDocumentBuilder SafeUnflatten(JsonDocumentBuilder value)
        {
            if (value.ValueKind != JsonValueKind.Object || value.GetObjectLength() == 0)
            {
                return value;
            }
            var safe = true;
            var index = 0;
            foreach (var item in value.EnumerateObject())
            {
                if (int.TryParse(item.Key, out var n) && index++ == n) continue;
                safe = false;
                break;
            }

            if (safe)
            {
                var j = new JsonDocumentBuilder(JsonValueKind.Array);
                foreach (var item in value.EnumerateObject())
                {
                    j.AddArrayItem(item.Value);
                }
                var a = new JsonDocumentBuilder(JsonValueKind.Array);
                foreach (var item in j.EnumerateArray())
                {
                    a.AddArrayItem(SafeUnflatten(item));
                }
                return a;
            }

            var o = new JsonDocumentBuilder(JsonValueKind.Object);
            foreach (var item in value.EnumerateObject())
            {
                //if (!o.ContainsPropertyName(item.Key))
                //{
                //    o.AddProperty(item.Key, SafeUnflatten (item.Value));
                //}
                o.TryAddProperty(item.Key, SafeUnflatten (item.Value));
            }
            return o;
        }

        private static bool TryUnflattenArray(JsonElement value, out JsonDocumentBuilder result)
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("The value to unflatten is not a JSON object");
            }

            result = new JsonDocumentBuilder(JsonValueKind.Object);

            foreach (var item in value.EnumerateObject())
            {
                JsonDocumentBuilder? parent = null;
                var part = result;
                var parentIndex = 0;
                var parentName = "";

                if (!JsonPointer.TryParse(item.Name, out var ptr))
                {
                    throw new ArgumentException("Name contains invalid JSON Pointer");
                }
                var index = 0;

                using var it = ptr.GetEnumerator();
                var more = it.MoveNext();
                while (more)
                {
                    var token = it.Current;

                    if (int.TryParse(token, out var n) && index++ == n)
                    {
                        if (part.ValueKind != JsonValueKind.Array)
                        {
                            if (parent != null && parent.ValueKind == JsonValueKind.Object)
                            {
                                parent.RemoveProperty(parentName);
                                var val = new JsonDocumentBuilder(JsonValueKind.Array);
                                parent.AddProperty(parentName, val);
                                part = val;
                            }
                            else if (parent != null && parent.ValueKind == JsonValueKind.Array)
                            {
                                var val = new JsonDocumentBuilder(JsonValueKind.Array);
                                parent[parentIndex] = val;
                                part = val;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        parent = part;
                        parentIndex = n;
                        parentName = token;
                        more = it.MoveNext();
                        if (more)
                        {
                            if (n >= part.GetArrayLength())
                            {
                                part.AddArrayItem(new JsonDocumentBuilder(JsonValueKind.Object));
                                part = part[part.GetArrayLength() - 1];
                            }
                            else
                            {
                                part = part[n];
                            }
                        }
                        else
                        {
                            part.AddArrayItem(new JsonDocumentBuilder(item.Value));
                            part = part[part.GetArrayLength() - 1];
                        }
                    }
                    else if (part.ValueKind == JsonValueKind.Object)
                    {
                        more = it.MoveNext();
                        if (more)
                        {
                            if (part.TryGetProperty(token, out var val))
                            {
                                part = val;
                            }
                            else
                            {
                                val = new JsonDocumentBuilder(JsonValueKind.Object);
                                part.AddProperty(token,val);
                                part = val;
                            }
                        }
                        else
                        {
                            if (part.TryGetProperty(token, out var val))
                            {
                                part = val;
                            }
                            else
                            {
                                val = new JsonDocumentBuilder(item.Value);
                                part.AddProperty(token,val);
                                part = val;
                            }
                        }
                    }
                    else 
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static JsonDocumentBuilder UnflattenToObject(JsonElement value, IntegerTokenUnflattening options = IntegerTokenUnflattening.TryIndex)
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("The value to unflatten is not a JSON object");
            }

            var result = new JsonDocumentBuilder(JsonValueKind.Object);

            foreach (var item in value.EnumerateObject())
            {
                var part = result;
                if (!JsonPointer.TryParse(item.Name, out var ptr))
                {
                    throw new ArgumentException("Name contains invalid JSON Pointer");
                }

                using var it = ptr.GetEnumerator();
                var more = it.MoveNext();
                while (more)
                {
                    var s = it.Current;
                    more = it.MoveNext();
                    if (more)
                    {
                        if (part.TryGetProperty(s, out var val))
                        {
                            part = val;
                        }
                        else
                        {
                            val = new JsonDocumentBuilder(JsonValueKind.Object);
                            part.AddProperty(s,val);
                            part = val;
                        }
                    }
                    else
                    {
                        if (part.TryGetProperty(s, out var val))
                        {
                            part = val;
                        }
                        else
                        {
                            val = new JsonDocumentBuilder(item.Value);
                            part.AddProperty(s,val);
                            part = val;
                        }
                    }
                }
            }

            return options == IntegerTokenUnflattening.TryIndex ? SafeUnflatten (result) : result;
        }
    }
}
