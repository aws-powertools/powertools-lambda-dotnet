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

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     List of key-value pairs with Metric Dimensions
/// </summary>
public class DimensionSet
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DimensionSet" /> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public DimensionSet(string key, string value)
    {
        Dimensions[key] = value;
    }

    /// <summary>
    ///     Gets the dimensions.
    /// </summary>
    /// <value>The dimensions.</value>
    internal Dictionary<string, string> Dimensions { get; } = new();

    /// <summary>
    ///     Gets the dimension keys.
    /// </summary>
    /// <value>The dimension keys.</value>
    public List<string> DimensionKeys => Dimensions.Keys.ToList();
}