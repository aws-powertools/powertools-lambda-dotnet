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

namespace AWS.Lambda.Powertools.Parameters.Transform;

internal class TransformerManager : ITransformerManager
{
    private static ITransformerManager? _instance;
    internal static ITransformerManager Instance => _instance ??= new TransformerManager();
    
    private readonly ConcurrentDictionary<string, ITransformer> _transformers = new(StringComparer.OrdinalIgnoreCase);

    private readonly ITransformer _jsonTransformer;
    private readonly ITransformer _base64Transformer;

    internal TransformerManager()
    {
        _jsonTransformer = new JsonTransformer();
        _base64Transformer = new Base64Transformer();
    }
    
    public ITransformer GetTransformer(Transformation transformation)
    {
        var transformer = TryGetTransformer(transformation, string.Empty);
        if (transformer is null)
            throw new NotSupportedException("'Transformation.Auto' requires additional 'key' parameter");
        return transformer;
    }

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

    public ITransformer GetTransformer(string transformerName)
    {
        var transformer = TryGetTransformer(transformerName);
        if (transformer is null)
            throw new KeyNotFoundException($"Transformer with name '{transformerName}' not found.");
        return transformer;
    }

    public ITransformer? TryGetTransformer(string transformerName)
    {
        if (string.IsNullOrWhiteSpace(transformerName))
            throw new ArgumentException("transformerName is required.");
        _transformers.TryGetValue(transformerName, out var transformer);
        return transformer;
    }

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