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
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common;
using Xunit;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

[Collection("Sequential")]
public class FunctionHandlerTests : IDisposable
{
    private readonly FunctionHandler _handler;
    private readonly CustomConsoleWriter _consoleOut;
    
    public FunctionHandlerTests()
    {
        _handler = new FunctionHandler();
        _consoleOut = new CustomConsoleWriter();
        SystemWrapper.Instance.SetOut(_consoleOut);
    }
    
    [Fact]
    public async Task When_Metrics_Add_Metadata_Same_Key_Should_Ignore_Metadata()
    {
        // Arrange
        
        
        // Act
        var exception = await Record.ExceptionAsync( () => _handler.HandleSameKey("whatever"));
        
        // Assert
        Assert.Null(exception);
    }
    
    [Fact]
    public async Task When_Metrics_Add_Metadata_Second_Invocation_Should_Not_Throw_Exception()
    {
        // Act
        var exception = await Record.ExceptionAsync( () => _handler.HandleTestSecondCall("whatever"));
        Assert.Null(exception);
        
        exception = await Record.ExceptionAsync( () => _handler.HandleTestSecondCall("whatever"));
        Assert.Null(exception);
    }
    
    [Fact]
    public async Task When_Metrics_Add_Metadata_FromMultipleThread_Should_Not_Throw_Exception()
    {
        // Act
        var exception = await Record.ExceptionAsync(() => _handler.HandleMultipleThreads("whatever"));
        Assert.Null(exception);
    }
    
    [Fact]
    public void When_LambdaContext_Should_Add_FunctioName_Dimension_CaptureColdStart()
    {
        // Arrange
        var context = new TestLambdaContext
        {
            FunctionName = "My Function with context"
        };
        
        // Act
        _handler.HandleWithLambdaContext(context);
        var metricsOutput = _consoleOut.ToString();
        
        // Assert
        Assert.Contains(
            "\"FunctionName\":\"My Function with context\"",
            metricsOutput);
        
        Assert.Contains(
            "\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"FunctionName\",\"Service\"]]}]}",
            metricsOutput);
    }
    
    [Fact]
    public void When_LambdaContext_And_Parameter_Should_Add_FunctioName_Dimension_CaptureColdStart()
    {
        // Arrange
        var context = new TestLambdaContext
        {
            FunctionName = "My Function with context"
        };
        
        // Act
        _handler.HandleWithParamAndLambdaContext("Hello",context);
        var metricsOutput = _consoleOut.ToString();
        
        // Assert
        Assert.Contains(
            "\"FunctionName\":\"My Function with context\"",
            metricsOutput);
        
        Assert.Contains(
            "\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"FunctionName\",\"Service\"]]}]}",
            metricsOutput);
    }
    
    [Fact]
    public void When_No_LambdaContext_Should_Not_Add_FunctioName_Dimension_CaptureColdStart()
    {
        // Act
        _handler.HandleColdStartNoContext();
        var metricsOutput = _consoleOut.ToString();
        
        // Assert
        Assert.DoesNotContain(
            "\"FunctionName\"",
            metricsOutput);
        
        Assert.Contains(
            "\"Metrics\":[{\"Name\":\"MyMetric\",\"Unit\":\"None\"}],\"Dimensions\":[[\"Service\"]]}]},\"Service\":\"svc\",\"MyMetric\":1}",
            metricsOutput);
    }

    public void Dispose()
    {
        Metrics.ResetForTest();
        MetricsAspect.ResetForTest();
    }
}