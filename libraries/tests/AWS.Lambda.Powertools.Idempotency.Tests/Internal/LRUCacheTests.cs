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
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Idempotency.Internal;
using Xunit;

//Source: https://github.dev/microsoft/botbuilder-dotnet/blob/main/tests/AdaptiveExpressions.Tests/LRUCacheTest.cs
namespace AWS.Lambda.Powertools.Idempotency.Tests.Internal;

public class LRUCacheTests
{
    [Fact]
    public void TestBasic()
    {
        var cache = new LRUCache<int, string>(2);

        Assert.False(cache.TryGet(1, out var result));

        cache.Set(1, "num1");

        Assert.True(cache.TryGet(1, out result));
        Assert.Equal("num1", result);
    }

    [Fact]
    public void TestDiacardPolicy()
    {
        var cache = new LRUCache<int, string>(2);
        cache.Set(1, "num1");
        cache.Set(2, "num2");
        cache.Set(3, "num3");

        // should be {2,'num2'} and {3, 'num3'}
        Assert.False(cache.TryGet(1, out var result));

        Assert.True(cache.TryGet(2, out result));
        Assert.Equal("num2", result);

        Assert.True(cache.TryGet(3, out result));
        Assert.Equal("num3", result);
    }

    [Fact]
    /*
     * The average time of this test is about 2ms. 
     */
    public void TestDPMemorySmall()
    {
        var cache = new LRUCache<int, int>(2);
        cache.Set(0, 1);
        cache.Set(1, 1);
        const int fib9999 = 1242044891;
        const int fib100000 = 2132534333;
        const int maxIdx = 10000;
        for (var i = 2; i <= maxIdx; i++)
        {
            cache.TryGet(i - 2, out var prev2);
            cache.TryGet(i - 1, out var prev1);
            cache.Set(i, prev1 + prev2);
        }

        Assert.False(cache.TryGet(9998, out var result));

        Assert.True(cache.TryGet(maxIdx - 1, out result));
        Assert.Equal(result, fib9999);

        Assert.True(cache.TryGet(maxIdx, out result));
        Assert.Equal(result, fib100000);
    }

    
    /*
     * The average time of this test is about 3ms. 
     */
    [Fact]
    public void TestDPMemoryLarge()
    {
        var cache = new LRUCache<int, int>(500);
        cache.Set(0, 1);
        cache.Set(1, 1);
        const int fib9999 = 1242044891;
        const int fib100000 = 2132534333;
        const int maxIdx = 10000;
        for (var i = 2; i <= 10000; i++)
        {
            cache.TryGet(i - 2, out var prev2);
            cache.TryGet(i - 1, out var prev1);
            cache.Set(i, prev1 + prev2);
        }

        Assert.False(cache.TryGet(1, out var result));

        Assert.True(cache.TryGet(maxIdx - 1, out result));
        Assert.Equal(result, fib9999);

        Assert.True(cache.TryGet(maxIdx, out result));
        Assert.Equal(result, fib100000);
    }

    [Fact]
    /*
     * The average time of this test is about 13ms(without the loop of Assert statements). 
     */
    public async Task TestMultiThreadingAsync()
    {
        var cache = new LRUCache<int, int>(10);
        var tasks = new List<Task>();
        const int numOfThreads = 10;
        const int numOfOps = 1000;
        for (var i = 0; i < numOfThreads; i++)
        {
            tasks.Add(Task.Run(() => StoreElement(cache, numOfOps, i)));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        for (var i = numOfOps - numOfThreads; i < numOfOps; i++)
        {
            Assert.True(cache.TryGet(i, out var result));
        }
    }
    
    
    [Fact]
    public void TestDelete()
    {
        var cache = new LRUCache<int, string>(3);
        cache.Set(1, "num1");
        cache.Set(2, "num2");
        cache.Set(3, "num3");

        cache.Delete(1);
        
        // should be {2,'num2'} and {3, 'num3'}
        Assert.False(cache.TryGet(1, out var result));

        Assert.True(cache.TryGet(2, out result));
        Assert.Equal("num2", result);

        Assert.True(cache.TryGet(3, out result));
        Assert.Equal("num3", result);
    }

    private void StoreElement(LRUCache<int, int> cache, int numOfOps, int idx)
    {
        for (var i = 0; i < numOfOps; i++)
        {
            var key = i;
            var value = i;
            cache.Set(key, value);
        }
    }
}