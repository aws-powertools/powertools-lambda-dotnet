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
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AWS.Lambda.Powertools.JMESPath.Utilities
{
    /// <summary>
    /// Captures error message and the operation that caused it.
    /// </summary>
    public sealed class JsonPatchException : Exception
    {
        /// <summary>
        /// Constructs a <see cref="JsonPatchException"/>.
        /// </summary>
        /// <param name="operation">The operation that caused the error.</param>
        /// <param name="message">The error message.</param>
        public JsonPatchException(
            string operation,
            string message) : base(message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Operation = operation;
        }

        /// <summary>
        /// Gets the <see cref="string"/> that caused the error.
        /// </summary>
        public string Operation { get; }
    }

    /// <summary>
    /// Provides functionality for applying a JSON Patch as 
    /// defined in <see href="https://datatracker.ietf.org/doc/html/rfc6902">RFC 6902</see>
    /// to a JSON value.
    /// </summary>
    /// <example>
    /// The following example borrowed from [jsonpatch.com](http://jsonpatch.com/) shows how to apply a JSON Patch to a JSON value
    /// <code>
    /// using System;
    /// using System.Diagnostics;
    /// using System.Text.Json;
    /// 
    /// public class Example
    /// {
    ///    public static void Main()
    ///    {
    ///     using var doc = JsonDocument.Parse(@"
    /// {
    /// ""baz"": ""qux"",
    /// ""foo"": ""bar""
    /// }
    ///     ");
    /// 
    ///     using var patch = JsonDocument.Parse(@"
    /// [
    /// { ""op"": ""replace"", ""path"": ""/baz"", ""value"": ""boo"" },
    /// { ""op"": ""add"", ""path"": ""/hello"", ""value"": [""world""] },
    /// { ""op"": ""remove"", ""path"": ""/foo"" }
    /// ]
    ///     ");
    /// 
    ///     using JsonDocument result = JsonPatch.ApplyPatch(doc.RootElement, patch.RootElement);
    /// 
    ///     var options = new JsonSerializerOptions() { WriteIndented = true };
    /// 
    ///     Console.WriteLine("The original document:\n");
    ///     Console.WriteLine($"{JsonSerializer.Serialize(doc, options)}\n");
    ///     Console.WriteLine("The patch:\n");
    ///     Console.WriteLine($"{JsonSerializer.Serialize(patch, options)}\n");
    ///     Console.WriteLine("The result:\n");
    ///     Console.WriteLine($"{JsonSerializer.Serialize(result, options)}\n");
    ///        ");
    ///     }
    /// }
    /// </code>
    /// The original document:
    /// 
    /// <code>
    /// {
    ///   "baz": "qux",
    ///   "foo": "bar"
    /// }
    /// </code>
    /// 
    /// The patch:
    /// <code>
    /// 
    /// [
    ///   {
    ///     "op": "replace",
    ///     "path": "/baz",
    ///     "value": "boo"
    ///   },
    ///   {
    ///     "op": "add",
    ///     "path": "/hello",
    ///     "value": [
    ///       "world"
    ///     ]
    ///   },
    ///   {
    ///     "op": "remove",
    ///     "path": "/foo"
    ///   }
    /// ]
    /// </code>
    /// 
    /// The result:
    /// <code>
    /// {
    ///   "baz": "boo",
    ///   "hello": [
    ///     "world"
    ///   ]
    /// }
    /// </code>
    /// </example>
    public static class JsonPatch
    {
        /// <summary>
        /// Applies a JSON Patch as defined in <see href="https://datatracker.ietf.org/doc/html/rfc6902">RFC 6902</see> 
        /// to a source JSON value.
        /// </summary>
        /// <remarks>
        /// It is the users responsibility to properly Dispose the returned <see cref="JsonDocument"/> value
        /// </remarks>
        /// <param name="source">The source JSON value.</param>
        /// <param name="patch">The patch to be applied to the source JSON value.</param>
        /// <returns>The patched JSON value</returns>
        /// <exception cref="ArgumentException">
        /// The provided <paramref name="patch"/> is invalid 
        /// </exception>
        /// <exception cref="JsonPatchException">
        ///   A JSON Patch operation failed
        /// </exception>
        public static JsonDocument ApplyPatch(JsonElement source,
            JsonElement patch)
        {
            var documentBuilder = new JsonDocumentBuilder(source);
            ApplyPatch(ref documentBuilder, patch);
            return documentBuilder.ToJsonDocument();
        }

        private static void ApplyPatch(ref JsonDocumentBuilder target,
            JsonElement patch)
        {
            var comparer = JsonElementEqualityComparer.Instance;

            Debug.Assert(target != null);

            if (patch.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("Patch must be an array");
            }

            foreach (var operation in patch.EnumerateArray())
            {
                if (!operation.TryGetProperty("op", out var opElement))
                {
                    throw new ArgumentException("Invalid patch");
                }

                var op = opElement.GetString() ?? throw new InvalidOperationException("Operation cannot be null");

                if (!operation.TryGetProperty("path", out var pathElement))
                {
                    throw new ArgumentException(op, nameof(patch));
                }

                var path = pathElement.GetString() ?? throw new InvalidOperationException("Operation cannot be null");

                if (!JsonPointer.TryParse(path, out var location))
                {
                    throw new ArgumentException(op, nameof(patch));
                }

                switch (op)
                {
                    case "test":
                    {
                        if (!operation.TryGetProperty("value", out var value))
                        {
                            throw new ArgumentException(op, nameof(patch));
                        }

                        if (!location.TryGetValue(target, out var tested))
                        {
                            throw new ArgumentException(op, nameof(patch));
                        }

                        using var doc = tested.ToJsonDocument();
                        if (!comparer.Equals(doc.RootElement, value))
                        {
                            throw new JsonPatchException(op, "Test failed");
                        }

                        break;
                    }
                    case "add":
                    {
                        if (!operation.TryGetProperty("value", out var value))
                        {
                            throw new ArgumentException(op, nameof(patch));
                        }

                        var valueBuilder = new JsonDocumentBuilder(value);
                        if (location.TryAddIfAbsent(ref target, valueBuilder)) continue; // try insert without replace
                        if (!location.TryReplace(ref target, valueBuilder)) // try insert without replace
                        {
                            throw new JsonPatchException(op, "Add failed");
                        }

                        break;
                    }
                    case "remove" when !location.TryRemove(ref target):
                        throw new JsonPatchException(op, "Add failed");
                    case "replace":
                    {
                        if (!operation.TryGetProperty("value", out var value))
                        {
                            throw new ArgumentException(op, nameof(patch));
                        }

                        var valueBuilder = new JsonDocumentBuilder(value);
                        if (!location.TryReplace(ref target, valueBuilder))
                        {
                            throw new JsonPatchException(op, "Replace failed");
                        }

                        break;
                    }
                    case "move":
                    {
                        if (!operation.TryGetProperty("from", out var fromElement))
                        {
                            throw new ArgumentException(op, nameof(patch));
                        }

                        var from = fromElement.GetString() ??
                                   throw new InvalidOperationException("From element cannot be null");
                        
                        if (!JsonPointer.TryParse(from, out var fromPointer))
                        {
                            throw new ArgumentException(op, nameof(patch));
                        }

                        if (!fromPointer.TryGetValue(target, out var value))
                        {
                            throw new JsonPatchException(op, "Move failed");
                        }

                        if (!fromPointer.TryRemove(ref target))
                        {
                            throw new JsonPatchException(op, "Move failed");
                        }

                        if (!location.TryAddIfAbsent(ref target, value))
                        {
                            if (!location.TryReplace(ref target, value)) // try insert without replace
                            {
                                throw new JsonPatchException(op, "Move failed");
                            }
                        }

                        break;
                    }
                    case "copy":
                    {
                        if (!operation.TryGetProperty("from", out var fromElement))
                        {
                            throw new ArgumentException(op, nameof(patch));
                        }

                        var from = fromElement.GetString() ??
                                   throw new InvalidOperationException("from cannot be null");
                        if (!JsonPointer.TryParse(from, out var fromPointer))
                        {
                            throw new ArgumentException(op, nameof(patch));
                        }

                        if (!fromPointer.TryGetValue(target, out var value))
                        {
                            throw new JsonPatchException(op, "Copy failed");
                        }

                        if (!location.TryAddIfAbsent(ref target, value))
                        {
                            if (!location.TryReplace(ref target, value)) // try insert without replace
                            {
                                throw new JsonPatchException(op, "Move failed");
                            }
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Builds a JSON Patch as defined in <see href="https://datatracker.ietf.org/doc/html/rfc6902">RFC 6902</see> 
        /// given two JSON values, a source and a target.
        /// </summary>
        /// <remarks>
        /// It is the users responsibility to properly Dispose the returned <see cref="JsonDocument"/> value
        /// </remarks>
        /// <param name="source">The source JSON value.</param>
        /// <param name="target">The target JSON value.</param>
        /// <returns>A JSON Merge Patch to convert the source JSON value to the target JSON value</returns>
        public static JsonDocument FromDiff(JsonElement source,
            JsonElement target)
        {
            return _FromDiff(source, target, "").ToJsonDocument();
        }

        private static JsonDocumentBuilder _FromDiff(JsonElement source,
            JsonElement target,
            string path)
        {
            var builder = new JsonDocumentBuilder(JsonValueKind.Array);

            var comparer = JsonElementEqualityComparer.Instance;

            if (comparer.Equals(source, target))
            {
                return builder;
            }

            if (source.ValueKind == JsonValueKind.Array && target.ValueKind == JsonValueKind.Array)
            {
                var common = Math.Min(source.GetArrayLength(), target.GetArrayLength());
                for (var i = 0; i < common; ++i)
                {
                    var buffer = new StringBuilder(path);
                    buffer.Append("/");
                    buffer.Append(i.ToString());
                    var tempDiff = _FromDiff(source[i], target[i], buffer.ToString());
                    foreach (var item in tempDiff.EnumerateArray())
                    {
                        builder.AddArrayItem(item);
                    }
                }

                // Element in source, not in target - remove
                for (var i = source.GetArrayLength(); i-- > target.GetArrayLength();)
                {
                    var buffer = new StringBuilder(path);
                    buffer.Append('/');
                    buffer.Append(i.ToString());
                    var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                    valBuilder.AddProperty("op", new JsonDocumentBuilder("remove"));
                    valBuilder.AddProperty("path", new JsonDocumentBuilder(buffer.ToString()));
                    builder.AddArrayItem(valBuilder);
                }

                // Element in target, not in source - add, 
                for (var i = source.GetArrayLength(); i < target.GetArrayLength(); ++i)
                {
                    var a = target[i];
                    var buffer = new StringBuilder(path);
                    buffer.Append("/");
                    buffer.Append(i.ToString());
                    var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                    valBuilder.AddProperty("op", new JsonDocumentBuilder("add"));
                    valBuilder.AddProperty("path", new JsonDocumentBuilder(buffer.ToString()));
                    valBuilder.AddProperty("value", new JsonDocumentBuilder(a));
                    builder.AddArrayItem(valBuilder);
                }
            }
            else if (source.ValueKind == JsonValueKind.Object && target.ValueKind == JsonValueKind.Object)
            {
                foreach (var a in source.EnumerateObject())
                {
                    var buffer = new StringBuilder(path);
                    buffer.Append("/");
                    buffer.Append(JsonPointer.Escape(a.Name));

                    if (target.TryGetProperty(a.Name, out var element))
                    {
                        var tempDiff = _FromDiff(a.Value, element, buffer.ToString());
                        foreach (var item in tempDiff.EnumerateArray())
                        {
                            builder.AddArrayItem(item);
                        }
                    }
                    else
                    {
                        var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                        valBuilder.AddProperty("op", new JsonDocumentBuilder("remove"));
                        valBuilder.AddProperty("path", new JsonDocumentBuilder(buffer.ToString()));
                        builder.AddArrayItem(valBuilder);
                    }
                }

                foreach (var a in target.EnumerateObject())
                {
                    if (source.TryGetProperty(a.Name, out _)) continue;

                    var buffer = new StringBuilder(path);
                    buffer.Append("/");
                    buffer.Append(JsonPointer.Escape(a.Name));
                    var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                    valBuilder.AddProperty("op", new JsonDocumentBuilder("add"));
                    valBuilder.AddProperty("path", new JsonDocumentBuilder(buffer.ToString()));
                    valBuilder.AddProperty("value", new JsonDocumentBuilder(a.Value));
                    builder.AddArrayItem(valBuilder);
                }
            }
            else
            {
                var valBuilder = new JsonDocumentBuilder(JsonValueKind.Object);
                valBuilder.AddProperty("op", new JsonDocumentBuilder("replace"));
                valBuilder.AddProperty("path", new JsonDocumentBuilder(path));
                valBuilder.AddProperty("value", new JsonDocumentBuilder(target));
                builder.AddArrayItem(valBuilder);
            }

            return builder;
        }
    }
}