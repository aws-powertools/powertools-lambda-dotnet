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

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using AWS.Lambda.PowerTools.Metrics.Serializer;

namespace AWS.Lambda.PowerTools.Metrics;

/// <summary>
///     EMF MetricUnit object types
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum MetricUnit
{
    /// <summary>
    ///     The none
    /// </summary>
    [EnumMember(Value = "None")] NONE,

    /// <summary>
    ///     The seconds
    /// </summary>
    [EnumMember(Value = "Seconds")] SECONDS,

    /// <summary>
    ///     The microseconds
    /// </summary>
    [EnumMember(Value = "Microseconds")] MICROSECONDS,

    /// <summary>
    ///     The milliseconds
    /// </summary>
    [EnumMember(Value = "Milliseconds")] MILLISECONDS,

    /// <summary>
    ///     The bytes
    /// </summary>
    [EnumMember(Value = "Bytes")] BYTES,

    /// <summary>
    ///     The kilobytes
    /// </summary>
    [EnumMember(Value = "Kilobytes")] KILOBYTES,

    /// <summary>
    ///     The megabytes
    /// </summary>
    [EnumMember(Value = "Megabytes")] MEGABYTES,

    /// <summary>
    ///     The gigabytes
    /// </summary>
    [EnumMember(Value = "Gigabytes")] GIGABYTES,

    /// <summary>
    ///     The terabytes
    /// </summary>
    [EnumMember(Value = "Terabytes")] TERABYTES,

    /// <summary>
    ///     The bits
    /// </summary>
    [EnumMember(Value = "Bits")] BITS,

    /// <summary>
    ///     The kilobits
    /// </summary>
    [EnumMember(Value = "Kilobits")] KILOBITS,

    /// <summary>
    ///     The megabits
    /// </summary>
    [EnumMember(Value = "Megabits")] MEGABITS,

    /// <summary>
    ///     The gigabits
    /// </summary>
    [EnumMember(Value = "Gigabits")] GIGABITS,

    /// <summary>
    ///     The terabits
    /// </summary>
    [EnumMember(Value = "Terabits")] TERABITS,

    /// <summary>
    ///     The percent
    /// </summary>
    [EnumMember(Value = "Percent")] PERCENT,

    /// <summary>
    ///     The count
    /// </summary>
    [EnumMember(Value = "Count")] COUNT,

    /// <summary>
    ///     The bytes per second
    /// </summary>
    [EnumMember(Value = "Bytes/Second")] BYTES_PER_SECOND,

    /// <summary>
    ///     The kilobytes per second
    /// </summary>
    [EnumMember(Value = "Kilobytes/Second")]
    KILOBYTES_PER_SECOND,

    /// <summary>
    ///     The megabytes per second
    /// </summary>
    [EnumMember(Value = "Megabytes/Second")]
    MEGABYTES_PER_SECOND,

    /// <summary>
    ///     The gigabytes per second
    /// </summary>
    [EnumMember(Value = "Gigabytes/Second")]
    GIGABYTES_PER_SECOND,

    /// <summary>
    ///     The terabytes per second
    /// </summary>
    [EnumMember(Value = "Terabytes/Second")]
    TERABYTES_PER_SECOND,

    /// <summary>
    ///     The bits per second
    /// </summary>
    [EnumMember(Value = "Bits/Second")] BITS_PER_SECOND,

    /// <summary>
    ///     The kilobits per second
    /// </summary>
    [EnumMember(Value = "Kilobits/Second")]
    KILOBITS_PER_SECOND,

    /// <summary>
    ///     The megabits per second
    /// </summary>
    [EnumMember(Value = "Megabits/Second")]
    MEGABITS_PER_SECOND,

    /// <summary>
    ///     The gigabits per second
    /// </summary>
    [EnumMember(Value = "Gigabits/Second")]
    GIGABITS_PER_SECOND,

    /// <summary>
    ///     The terabits per second
    /// </summary>
    [EnumMember(Value = "Terabits/Second")]
    TERABITS_PER_SECOND,

    /// <summary>
    ///     The count per second
    /// </summary>
    [EnumMember(Value = "Count/Second")] COUNT_PER_SECOND
}