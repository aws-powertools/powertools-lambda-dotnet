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

using System.Diagnostics.CodeAnalysis;
using AWS.Lambda.Powertools.Parameters.Cache;

namespace AWS.Lambda.Powertools.Parameters.Internal.Cache;

[ExcludeFromCodeCoverage]
internal class DateTimeWrapper : IDateTimeWrapper
{
    private static IDateTimeWrapper? _instance;

    internal static IDateTimeWrapper Instance => _instance ??= new DateTimeWrapper();

    private DateTimeWrapper()
    {
    }

    public DateTime UtcNow => DateTime.UtcNow;
}