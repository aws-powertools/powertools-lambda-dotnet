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
using AWS.Lambda.Powertools.Idempotency.Persistence;
using FluentAssertions;
using Xunit;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Persistence;

public class DataRecordTests
{
    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenCurrentTimeIsGreaterThanExpiryTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var dataRecord = new DataRecord(
            "123", 
            DataRecord.DataRecordStatus.INPROGRESS, 
            now.AddSeconds(-1).ToUnixTimeSeconds(),
            "abc","123");
        dataRecord.IsExpired(now).Should().BeTrue();
    }
    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenCurrentTimeIsLessThanExpiryTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var dataRecord = new DataRecord(
            "123", 
            DataRecord.DataRecordStatus.INPROGRESS, 
            now.AddSeconds(10).ToUnixTimeSeconds(),
            "abc","123");
        dataRecord.IsExpired(now).Should().BeFalse();
    }
    
    [Fact]
    public void Status_ShouldBeExpired_WhenCurrentTimeIsGreaterThanExpiryTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var dataRecord = new DataRecord(
            "123", 
            DataRecord.DataRecordStatus.INPROGRESS, 
            now.AddSeconds(-10).ToUnixTimeSeconds(),
            "abc","123");
        dataRecord.Status.Should().Be(DataRecord.DataRecordStatus.EXPIRED);
    }
    
    [Fact]
    public void Status_ShouldBeRecordStatus_WhenCurrentTimeDidnotExpire()
    {
        var now = DateTimeOffset.UtcNow;
        var dataRecord = new DataRecord(
            "123", 
            DataRecord.DataRecordStatus.INPROGRESS, 
            now.AddSeconds(10).ToUnixTimeSeconds(),
            "abc","123");
        dataRecord.Status.Should().Be(DataRecord.DataRecordStatus.INPROGRESS);
    }
}