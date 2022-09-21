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

using AWS.Lambda.Powertools.Parameters.Cache;
using Moq;
using Xunit;

namespace AWS.Lambda.Powertools.Parameters.Tests.Cache;

public class CacheManagerTest
{
    [Fact]
    public void Get_WhenCachedObjectExists_ReturnsCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var currentTime = DateTime.Now.AddHours(1);
        var duration = TimeSpan.FromSeconds(5);

        var dateTimeWrapper = new Mock<IDateTimeWrapper>();
        dateTimeWrapper.Setup(c => c.UtcNow).Returns(currentTime);

        var cacheManager = new CacheManager(dateTimeWrapper.Object);
        cacheManager.Set(key, value, duration);

        // Act
        var result = cacheManager.Get(key);

        // Assert
        dateTimeWrapper.Verify(v => v.UtcNow, Times.Exactly(2));
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public void Get_WhenCachedObjectDoesNotExist_ReturnsNull()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var currentTime = DateTime.Now.AddHours(1);

        var dateTimeWrapper = new Mock<IDateTimeWrapper>();
        dateTimeWrapper.Setup(c => c.UtcNow).Returns(currentTime);

        var cacheManager = new CacheManager(dateTimeWrapper.Object);

        // Act
        var result = cacheManager.Get(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Get_WhenCachedObjectDoesNotExpire_ReturnsCachedObject()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var currentTime = DateTime.Now.AddHours(1);
        var duration = TimeSpan.FromSeconds(5);

        var dateTimeWrapper = new Mock<IDateTimeWrapper>();
        dateTimeWrapper.Setup(c => c.UtcNow).Returns(currentTime);

        var cacheManager = new CacheManager(dateTimeWrapper.Object);
        cacheManager.Set(key, value, duration);

        currentTime = currentTime.Add(duration).AddSeconds(-1);
        dateTimeWrapper.Setup(c => c.UtcNow).Returns(currentTime);

        // Act
        var result = cacheManager.Get(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public void Get_WhenCachedObjectExpires_ReturnsNull()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();
        var value = Guid.NewGuid().ToString();
        var currentTime = DateTime.Now.AddHours(1);
        var duration = TimeSpan.FromSeconds(5);

        var dateTimeWrapper = new Mock<IDateTimeWrapper>();
        dateTimeWrapper.Setup(c => c.UtcNow).Returns(currentTime);

        var cacheManager = new CacheManager(dateTimeWrapper.Object);
        cacheManager.Set(key, value, duration);

        currentTime = currentTime.Add(duration).AddSeconds(1);
        dateTimeWrapper.Setup(c => c.UtcNow).Returns(currentTime);

        // Act
        var result = cacheManager.Get(key);

        // Assert
        Assert.Null(result);
    }
}