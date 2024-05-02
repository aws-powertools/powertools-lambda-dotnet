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
using System.Text.Json;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath
{
    // 2024-04-19: Powertools addition.
    internal sealed class DynamicResources;
    /// <summary>
    /// Provides functionality for applying a JMESPath expression to transform a JSON document into
    /// another JSON document
    /// </summary>
    /// <example>
    /// The following example shows how to apply a JMESPath expression to transform a JSON document into
    /// another JSON document.
    /// <code>
    /// using System;
    /// using System.Text.Json;
    /// 
    /// public class Example
    /// {
    ///     public static void Main()
    ///     {
    ///    string jsonString = @"
    /// {
    /// ""people"": [
    /// {
    ///   ""age"": 20,
    ///   ""other"": ""foo"",
    ///   ""name"": ""Bob""
    /// },
    /// {
    ///   ""age"": 25,
    ///   ""other"": ""bar"",
    ///   ""name"": ""Fred""
    /// },
    /// {
    ///  ""age"": 30,
    ///  ""other"": ""baz"",
    ///  ""name"": ""George""
    /// }
    /// ]
    /// }
    ///    ";
    /// 
    ///    using JsonDocument doc = JsonDocument.Parse(jsonString);
    /// 
    ///    var transformer = JsonTransformer.Parse("people[?age > `20`].[name, age]");
    /// 
    ///    using JsonDocument result = transformer.Transform(doc.RootElement);
    /// 
    ///    var serializerOptions = new JsonSerializerOptions() {WriteIndented = true};
    ///    Console.WriteLine(JsonSerializer.Serialize(result.RootElement, serializerOptions));
    /// }
    /// </code>
    /// Output:
    /// 
    /// <code>
    /// [
    ///   [
    ///     "Fred",
    ///     25
    ///   ],
    ///   [
    ///     "George",
    ///     30
    ///   ]
    /// ]
    /// </code>
    /// </example>

    public sealed class JsonTransformer
    {
        /// <summary>
        /// Parses a JMESPath string into a <see cref="JsonTransformer"/>, for "parse once, use many times".
        /// A <see cref="JsonTransformer"/> instance is thread safe and has no mutable state.
        /// </summary>
        /// <param name="jmesPath">A JMESPath string.</param>
        /// <returns>A <see cref="JsonTransformer"/>.</returns>
        /// <exception cref="JmesPathParseException">
        ///   The <paramref name="jmesPath"/> parameter is not a valid JMESPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="jmesPath"/> is <see langword="null"/>.
        /// </exception>
        public static JsonTransformer Parse(string jmesPath)
        {
            if (jmesPath == null)
            {
                throw new ArgumentNullException(nameof(jmesPath));
            }
            var compiler = new JmesPathParser(jmesPath);
            return compiler.Parse();
        }

        private readonly Expression _expr;

        internal JsonTransformer(Expression expr)
        {
            _expr = expr;
        }

        /// <summary>
        /// Applies a JMESPath expression to a JSON document to transform it
        /// into another Json document.
        /// </summary>
        /// <remarks>
        /// It is the users responsibilty to properly Dispose the returned <see cref="JsonDocument"/> value
        /// </remarks>
        /// <param name="doc">The provided JSON document.</param>
        /// <returns>The transformed JSON document. If a type error is detected in a function call,
        /// a JSON null value is returned.</returns>
        public JsonDocument Transform(JsonElement doc)
        {
            // 2024-04-19: Powertools addition.
            var resources = new DynamicResources();
            _expr.TryEvaluate(resources, new JsonElementValue(doc), out var temp);
            return JsonDocument.Parse(temp.ToString() ?? string.Empty);
        }

        /// <summary>
        /// Applies a JMESPath expression to a JSON document to transform it
        /// into another Json document.
        /// This method parses and applies the expression in one operation.
        /// </summary>
        /// <remarks>
        /// It is the users responsibilty to properly Dispose the returned <see cref="JsonDocument"/> value
        /// </remarks>
        /// <param name="doc">The provided JSON document.</param>
        /// <param name="jmesPath">A JMESPath string.</param>
        /// <returns>The transformed JSON document.</returns>
        /// <exception cref="JmesPathParseException">
        ///   The <paramref name="jmesPath"/> parameter is not a valid JMESPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="jmesPath"/> is <see langword="null"/>.
        /// </exception>
        public static JsonDocument Transform(JsonElement doc, string jmesPath)
        {
            // 2024-04-19: Powertools addition.
            var searcher = Parse(jmesPath); 
            return searcher.Transform(doc);
        }       
    }
}
