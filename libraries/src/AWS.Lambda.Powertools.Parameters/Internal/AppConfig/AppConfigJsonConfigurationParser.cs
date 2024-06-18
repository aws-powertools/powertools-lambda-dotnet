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

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AWS.Lambda.Powertools.Parameters.Internal.AppConfig;

/// <summary>
/// AppConfigJsonConfigurationParser class
/// </summary>
internal class AppConfigJsonConfigurationParser
{
    /// <summary>
    /// The processed data.
    /// </summary>
    private readonly IDictionary<string, string?> _data =
        new SortedDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Stack for processing the document.
    /// </summary>
    private readonly Stack<string> _context = new();
    
    /// <summary>
    /// Pointer to the current path.
    /// </summary>
    private string _currentPath = string.Empty;

    /// <summary>
    /// Parse dictionary from AppConfig JSON stream.
    /// </summary>
    /// <param name="input">AppConfig JSON stream.</param>
    /// <returns>JSON Dictionary.</returns>
    public static IDictionary<string, string?> Parse(Stream input)
    {
        using var doc = JsonDocument.Parse(input);
        var parser = new AppConfigJsonConfigurationParser();
        parser.VisitElement(doc.RootElement);
        return parser._data;
    }

    /// <summary>
    /// Parse dictionary from AppConfig JSON string.
    /// </summary>
    /// <param name="input">AppConfig JSON string.</param>
    /// <returns>JSON Dictionary.</returns>
    public static IDictionary<string, string?> Parse(string input)
    {
        using var doc = JsonDocument.Parse(input);
        var parser = new AppConfigJsonConfigurationParser();
        parser.VisitElement(doc.RootElement);
        return parser._data;
    }

    /// <summary>
    /// Process single JSON element.
    /// </summary>
    /// <param name="element">The JSON element</param>
    private void VisitElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Undefined:
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    EnterContext(property.Name);
                    VisitElement(property.Value);
                    ExitContext();
                }

                break;
            case JsonValueKind.Array:
                VisitArray(element);
                break;
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                VisitPrimitive(element);
                break;
            case JsonValueKind.Null:
                VisitNull();
                break;
        }
    }

    /// <summary>
    /// Process array JSON element.
    /// </summary>
    /// <param name="array">The JSON array</param>
    private void VisitArray(JsonElement array)
    {
        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            EnterContext(index.ToString(CultureInfo.InvariantCulture));
            VisitElement(item);
            ExitContext();

            index++;
        }
    }

    /// <summary>
    /// Process JSON null element.
    /// </summary>
    private void VisitNull()
    {
        var key = _currentPath;
        _data[key] = null;
    }

    /// <summary>
    /// Process JSON primitive element.
    /// </summary>
    /// <param name="data">The JSON element.</param>
    /// <exception cref="FormatException"></exception>
    private void VisitPrimitive(JsonElement data)
    {
        var key = _currentPath;

        if (_data.ContainsKey(key))
        {
            throw new FormatException($"A duplicate key '{key}' was found.");
        }

        _data[key] = data.ToString();
    }

    /// <summary>
    /// Enter into a context of a new element to process.
    /// </summary>
    /// <param name="context">The context</param>
    private void EnterContext(string context)
    {
        _context.Push(context);
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }

    /// <summary>
    /// Enter from context of an element which is processed.
    /// </summary>
    private void ExitContext()
    {
        _context.Pop();
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }
}