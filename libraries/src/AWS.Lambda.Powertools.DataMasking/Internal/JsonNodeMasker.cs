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
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.DataMasking.Internal;

/// <summary>
/// Applies masking to fields within a <see cref="JsonNode"/> tree using simple dotted field paths
/// (for example <c>"address.street"</c>).
/// </summary>
/// <remarks>
/// This intentionally operates on the <see cref="System.Text.Json.Nodes"/> object model rather than
/// reflecting over arbitrary CLR types, which keeps the operation trimming/AOT friendly.
/// Full JMESPath expression support (wildcards, filters) is planned for a later phase.
/// </remarks>
internal static class JsonNodeMasker
{
    private const char PathSeparator = '.';

    /// <summary>
    /// Masks every supplied field path within <paramref name="root"/> in place.
    /// </summary>
    /// <param name="root">The root node of the payload being masked.</param>
    /// <param name="fields">The dotted field paths to mask.</param>
    /// <param name="options">The masking options controlling the replacement.</param>
    internal static void MaskFields(JsonNode? root, IEnumerable<string> fields, MaskingOptions options)
    {
        if (root is null)
        {
            return;
        }

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                continue;
            }

            MaskPath(root, field, options);
        }
    }

    private static void MaskPath(JsonNode root, string path, MaskingOptions options)
    {
        if (TryResolveLeaf(root, path, out var parent, out var leaf))
        {
            parent[leaf] = MaskValue(parent[leaf], options);
        }
    }

    /// <summary>
    /// Walks the dotted <paramref name="path"/> and, if it resolves to a property on an object,
    /// returns that object and the leaf key. Returns <see langword="false"/> when any intermediate
    /// segment is missing or is not an object, or when the leaf property does not exist.
    /// </summary>
    /// <remarks>
    /// The walk avoids the intermediate <c>string[]</c> that <c>string.Split</c> would allocate.
    /// JsonObject lookups require a string key, so each segment is still materialized, but the array
    /// (and its bounds/GC overhead) is avoided.
    /// </remarks>
    private static bool TryResolveLeaf(JsonNode root, string path, out JsonObject parent, out string leaf)
    {
        parent = null!;
        leaf = string.Empty;

        var current = root;
        var start = 0;

        int separator;
        while ((separator = path.IndexOf(PathSeparator, start)) >= 0)
        {
            var segment = path.Substring(start, separator - start);
            if (current is not JsonObject obj || !obj.TryGetPropertyValue(segment, out var next) || next is null)
            {
                return false;
            }

            current = next;
            start = separator + 1;
        }

        if (current is not JsonObject leafParent)
        {
            return false;
        }

        var key = start == 0 ? path : path.Substring(start);
        if (!leafParent.ContainsKey(key))
        {
            return false;
        }

        parent = leafParent;
        leaf = key;
        return true;
    }

    private static string ReadRawValue(JsonNode value) =>
        value is JsonValue jsonValue ? jsonValue.ToString() : value.ToJsonString();

    private static JsonNode? MaskValue(JsonNode? value, MaskingOptions options)
    {
        // A null JSON value stays null; there is nothing sensitive to mask.
        if (value is null)
        {
            return null;
        }

        // The fixed-mask case (the default) ignores the original value, so avoid serializing
        // the (potentially large) subtree to a string just to discard it.
        if (!options.RequiresRawValue)
        {
            return JsonValue.Create(options.Apply(string.Empty));
        }

        return JsonValue.Create(options.Apply(ReadRawValue(value)));
    }

    /// <summary>
    /// Applies an asynchronous string transform (for example encrypt or decrypt) to each supplied
    /// field path within <paramref name="root"/>, in place. Missing paths are skipped.
    /// </summary>
    internal static async Task TransformFieldsAsync(
        JsonNode? root,
        IEnumerable<string> fields,
        Func<string, Task<string>> transform)
    {
        if (root is null)
        {
            return;
        }

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                continue;
            }

            await TransformPathAsync(root, field, transform);
        }
    }

    private static async Task TransformPathAsync(JsonNode root, string path, Func<string, Task<string>> transform)
    {
        if (TryResolveLeaf(root, path, out var parent, out var leaf) && parent[leaf] is not null)
        {
            var transformed = await transform(ReadRawValue(parent[leaf]!));
            parent[leaf] = JsonValue.Create(transformed);
        }
    }
}
