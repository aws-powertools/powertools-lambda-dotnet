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

namespace AWS.Lambda.Powertools.Parameters.Transform;

/// <summary>
/// Represents a type used to manage transformers.
/// </summary>
public interface ITransformerManager
{
    /// <summary>
    /// Gets an instance of transformer for the provided transformation type.
    /// </summary>
    /// <param name="transformation">Type of the transformation.</param>
    /// <returns>The transformer instance</returns>
    ITransformer GetTransformer(Transformation transformation);

    /// <summary>
    /// Gets an instance of transformer for the provided transformation type and parameter key. 
    /// </summary>
    /// <param name="transformation">Type of the transformation.</param>
    /// <param name="key">Parameter key, it's required for Transformation.Auto</param>
    /// <returns>The transformer instance</returns>
    ITransformer? TryGetTransformer(Transformation transformation, string key);

    /// <summary>
    /// Gets an instance of transformer for the provided transformer name. 
    /// </summary>
    /// <param name="transformerName">The unique name for the transformer</param>
    /// <returns>The transformer instance</returns>
    ITransformer GetTransformer(string transformerName);

    /// <summary>
    /// Gets an instance of transformer for the provided transformer name. 
    /// </summary>
    /// <param name="transformerName">The unique name for the transformer</param>
    /// <returns>The transformer instance</returns>
    ITransformer? TryGetTransformer(string transformerName);

    /// <summary>
    /// Add an instance of a transformer by a unique name
    /// </summary>
    /// <param name="transformerName">name of the transformer</param>
    /// <param name="transformer">the transformer instance</param>
    void AddTransformer(string transformerName, ITransformer transformer);
}