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
/// Represents a type used to transform a parameter value.
/// </summary>
public interface ITransformer
{
    /// <summary>
    /// Transforms the string value to specified type.
    /// </summary>
    /// <param name="value">Parameter value.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The transformed value.</returns>
    T? Transform<T>(string value);
}