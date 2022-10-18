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

using System.Collections.Concurrent;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Internal.Transform;

/// <summary>
/// The TransformerManager class to manage transformers.
/// </summary>
internal class TransformerManager : ITransformerManager
{
    /// <summary>
    /// The TransformerManager instance.
    /// </summary>
    private static ITransformerManager? _instance;
    
    /// <summary>
    /// The JsonTransformer instance.
    /// </summary>
    private readonly ITransformer _jsonTransformer;
    
    /// <summary>
    /// The Base64Transformer instance.
    /// </summary>
    private readonly ITransformer _base64Transformer;
    
    /// <summary>
    /// Gets the TransformerManager instance.
    /// </summary>
    internal static ITransformerManager Instance => _instance ??= new TransformerManager();

    /// <summary>
    /// Gets the list of transformer instances.
    /// </summary>
    private readonly ConcurrentDictionary<string, ITransformer> _transformers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// TransformerManager constructor.
    /// </summary>
    internal TransformerManager()
    {
        _jsonTransformer = new JsonTransformer();
        _base64Transformer = new Base64Transformer();
    }

    /// <summary>
    /// Gets an instance of transformer for the provided transformation type.
    /// </summary>
    /// <param name="transformation">Type of the transformation.</param>
    /// <returns>The transformer instance</returns>
    /// <exception cref="NotSupportedException"></exception>
    public ITransformer GetTransformer(Transformation transformation)
    {
        var transformer = TryGetTransformer(transformation, string.Empty);
        if (transformer is null)
            throw new NotSupportedException("'Transformation.Auto' requires additional 'key' parameter");
        return transformer;
    }

    /// <summary>
    /// Gets an instance of transformer for the provided transformation type and parameter key. 
    /// </summary>
    /// <param name="transformation">Type of the transformation.</param>
    /// <param name="key">Parameter key, it's required for Transformation.Auto</param>
    /// <returns>The transformer instance</returns>
    public ITransformer? TryGetTransformer(Transformation transformation, string key)
    {
        return transformation switch
        {
            Transformation.Json => _transformers.GetOrAdd("json", _jsonTransformer),
            Transformation.Base64 => _transformers.GetOrAdd("base64", _base64Transformer),
            Transformation.Auto when key.EndsWith(".json", StringComparison.CurrentCultureIgnoreCase) =>
                _transformers.GetOrAdd("json", _jsonTransformer),
            Transformation.Auto when key.EndsWith(".binary", StringComparison.CurrentCultureIgnoreCase) =>
                _transformers.GetOrAdd("base64", _base64Transformer),
            Transformation.Auto when key.EndsWith(".base64", StringComparison.CurrentCultureIgnoreCase) =>
                _transformers.GetOrAdd("base64", _base64Transformer),
            _ => null
        };
    }

    /// <summary>
    /// Gets an instance of transformer for the provided transformer name. 
    /// </summary>
    /// <param name="transformerName">The unique name for the transformer</param>
    /// <returns>The transformer instance</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public ITransformer GetTransformer(string transformerName)
    {
        var transformer = TryGetTransformer(transformerName);
        if (transformer is null)
            throw new KeyNotFoundException($"Transformer with name '{transformerName}' not found.");
        return transformer;
    }

    /// <summary>
    /// Gets an instance of transformer for the provided transformer name. 
    /// </summary>
    /// <param name="transformerName">The unique name for the transformer</param>
    /// <returns>The transformer instance</returns>
    /// <exception cref="ArgumentException"></exception>
    public ITransformer? TryGetTransformer(string transformerName)
    {
        if (string.IsNullOrWhiteSpace(transformerName))
            throw new ArgumentException("transformerName is required.");
        _transformers.TryGetValue(transformerName, out var transformer);
        return transformer;
    }

    /// <summary>
    /// Add an instance of a transformer by a unique name
    /// </summary>
    /// <param name="transformerName">name of the transformer</param>
    /// <param name="transformer">the transformer instance</param>
    /// <exception cref="ArgumentException"></exception>
    public void AddTransformer(string transformerName, ITransformer transformer)
    {
        if (string.IsNullOrWhiteSpace(transformerName))
            throw new ArgumentException("transformerName is required.");

        if (_transformers.ContainsKey(transformerName))
            _transformers[transformerName] = transformer;
        else
            _transformers.TryAdd(transformerName, transformer);
    }
}