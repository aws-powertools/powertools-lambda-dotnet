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
using System.Threading.Tasks;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

[Collection("Sequential")]
public class FunctionHandlerTests : IDisposable
{
    [Fact]
    public async Task When_Metrics_Add_Metadata_Same_Key_Should_Ignore_Metadata()
    {
        // Arrange
        Metrics.ResetForTest();
        var handler = new FunctionHandler();
        
        // Act
        var exception = await Record.ExceptionAsync( () => handler.HandleSameKey("whatever"));
        
        // Assert
        Assert.Null(exception);
    }
    
    [Fact]
    public async Task When_Metrics_Add_Metadata_Second_Invocation_Should_Not_Throw_Exception()
    {
        // Arrange
        Metrics.ResetForTest();
        var handler = new FunctionHandler();

        // Act
        var exception = await Record.ExceptionAsync( () => handler.HandleTestSecondCall("whatever"));
        Assert.Null(exception);
        
        exception = await Record.ExceptionAsync( () => handler.HandleTestSecondCall("whatever"));
        Assert.Null(exception);
    }
    
    [Fact]
    public async Task When_Metrics_Add_Metadata_FromMultipleThread_Should_Not_Throw_Exception()
    {
        // Arrange
        Metrics.ResetForTest();
        var handler = new FunctionHandler();

        // Act
        var exception = await Record.ExceptionAsync(() => handler.HandleMultipleThreads("whatever"));
        Assert.Null(exception);
    }

    public void Dispose()
    {
        MetricsAspect.ResetForTest();
    }
}