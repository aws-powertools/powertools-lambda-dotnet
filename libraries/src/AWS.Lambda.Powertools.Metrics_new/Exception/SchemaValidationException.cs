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

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Class SchemaValidationException.
///     Implements the <see cref="System.Exception" />
/// </summary>
/// <seealso cref="System.Exception" />
[Serializable]
public class SchemaValidationException : Exception
{
    /// <summary>
    ///     Thrown when required property is missing on Metrics Object
    /// </summary>
    /// <param name="propertyName">Missing property name</param>
    public SchemaValidationException(string propertyName) : base(
        $"EMF schema is invalid. '{propertyName}' is mandatory and not specified.")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaValidationException" /> class.
    /// </summary>
    /// <param name="raiseEmptyMetrics">if set to <c>true</c> [raise empty metrics].</param>
    public SchemaValidationException(bool raiseEmptyMetrics) : base("No metrics have been provided.")
    {
    }
}