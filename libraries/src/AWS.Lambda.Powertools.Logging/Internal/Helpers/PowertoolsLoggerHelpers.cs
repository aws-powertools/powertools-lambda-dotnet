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

using System.Collections.Generic;
using System.Linq;

namespace AWS.Lambda.Powertools.Logging.Internal.Helpers;

/// <summary>
/// Class PowertoolsLoggerHelpers.
/// </summary>
internal static class PowertoolsLoggerHelpers
{
    /// <summary>
    /// Converts an object to a dictionary.
    /// </summary>
    /// <param name="anonymousObject">The object to convert.</param>
    /// <returns>
    /// If the object has a namespace, returns the object as-is.
    /// Otherwise, returns a dictionary representation of the object's properties.
    /// </returns>
    internal static object ObjectToDictionary(object anonymousObject)
    {
        if (anonymousObject == null)
        {
            return new Dictionary<string, object>();
        }

        if (anonymousObject.GetType().Namespace is not null)
        {
            return anonymousObject;
        }

        return anonymousObject.GetType().GetProperties()
            .Where(prop => prop.GetValue(anonymousObject, null) != null)
            .ToDictionary(
                prop => prop.Name,
                prop => {
                    var value = prop.GetValue(anonymousObject, null);
                    return value != null ? ObjectToDictionary(value) : string.Empty;
                }
            );
    }
}