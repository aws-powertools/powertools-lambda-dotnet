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

using System.Text;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Internal.Transform;

internal class Base64Transformer : ITransformer
{
    public T? Transform<T>(string value)
    {
        if (typeof(T) != typeof(string))
            return default;

        if (string.IsNullOrWhiteSpace(value))
            return (T)(object)value;

        // Base64 Decode
        var base64EncodedBytes = Convert.FromBase64String(value);
        value = Encoding.UTF8.GetString(base64EncodedBytes);
        return (T)(object)value;
    }
}