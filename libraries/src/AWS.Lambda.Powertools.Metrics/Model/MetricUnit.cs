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

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
/// Enum MetricUnit
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum MetricUnit
{
    /// <summary>
    ///     Metrics unit none
    /// </summary>
    [EnumMember(Value = "None")] None,

    /// <summary>
    ///     Metrics unit in seconds
    /// </summary>
    [EnumMember(Value = "Seconds")] Seconds,

    /// <summary>
    ///     Metrics unit in microseconds
    /// </summary>
    [EnumMember(Value = "Microseconds")] Microseconds,

    /// <summary>
    ///     Metrics unit in milliseconds
    /// </summary>
    [EnumMember(Value = "Milliseconds")] Milliseconds,

    /// <summary>
    ///     Metrics unit in bytes
    /// </summary>
    [EnumMember(Value = "Bytes")] Bytes,

    /// <summary>
    ///     Metrics unit in kilobytes
    /// </summary>
    [EnumMember(Value = "Kilobytes")] Kilobytes,

    /// <summary>
    ///     Metrics unit in megabytes
    /// </summary>
    [EnumMember(Value = "Megabytes")] Megabytes,

    /// <summary>
    ///     Metrics unit in gigabytes
    /// </summary>
    [EnumMember(Value = "Gigabytes")] Gigabytes,

    /// <summary>
    ///     Metrics unit in terabytes
    /// </summary>
    [EnumMember(Value = "Terabytes")] Terabytes,

    /// <summary>
    ///     Metrics unit in bits
    /// </summary>
    [EnumMember(Value = "Bits")] Bits,

    /// <summary>
    ///     Metrics unit in kilobits
    /// </summary>
    [EnumMember(Value = "Kilobits")] Kilobits,

    /// <summary>
    ///     Metrics unit in megabits
    /// </summary>
    [EnumMember(Value = "Megabits")] Megabits,

    /// <summary>
    ///     Metrics unit in gigabits
    /// </summary>
    [EnumMember(Value = "Gigabits")] Gigabits,

    /// <summary>
    ///     Metrics unit in terabits
    /// </summary>
    [EnumMember(Value = "Terabits")] Terabits,

    /// <summary>
    ///     Metrics unit in percent
    /// </summary>
    [EnumMember(Value = "Percent")] Percent,

    /// <summary>
    ///     Metrics unit count
    /// </summary>
    [EnumMember(Value = "Count")] Count,

    /// <summary>
    ///     Metrics unit in bytes per second
    /// </summary>
    [EnumMember(Value = "Bytes/Second")] BytesPerSecond,

    /// <summary>
    ///     Metrics unit in kilobytes per second
    /// </summary>
    [EnumMember(Value = "Kilobytes/Second")]
    KilobytesPerSecond,

    /// <summary>
    ///     Metrics unit in megabytes per second
    /// </summary>
    [EnumMember(Value = "Megabytes/Second")]
    MegabytesPerSecond,

    /// <summary>
    ///     Metrics unit in gigabytes per second
    /// </summary>
    [EnumMember(Value = "Gigabytes/Second")]
    GigabytesPerSecond,

    /// <summary>
    ///     Metrics unit in terabytes per second
    /// </summary>
    [EnumMember(Value = "Terabytes/Second")]
    TerabytesPerSecond,

    /// <summary>
    ///     Metrics unit in bits per second
    /// </summary>
    [EnumMember(Value = "Bits/Second")] BitsPerSecond,

    /// <summary>
    ///     Metrics unit in kilobits per second
    /// </summary>
    [EnumMember(Value = "Kilobits/Second")]
    KilobitsPerSecond,

    /// <summary>
    ///     Metrics unit in megabits per second
    /// </summary>
    [EnumMember(Value = "Megabits/Second")]
    MegabitsPerSecond,

    /// <summary>
    ///     Metrics unit in gigabits per second
    /// </summary>
    [EnumMember(Value = "Gigabits/Second")]
    GigabitsPerSecond,

    /// <summary>
    ///     Metrics unit in terabits per second
    /// </summary>
    [EnumMember(Value = "Terabits/Second")]
    TerabitsPerSecond,

    /// <summary>
    ///     Metrics unit in count per second
    /// </summary>
    [EnumMember(Value = "Count/Second")] CountPerSecond
}