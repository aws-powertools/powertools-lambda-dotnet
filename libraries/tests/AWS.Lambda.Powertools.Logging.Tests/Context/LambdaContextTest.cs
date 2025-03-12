using System;
using System.Reflection;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests.Context;

public class LambdaContextTest
{
     [Fact]
     public void Extract_WhenHasLambdaContextArgument_InitializesLambdaContextInfo()
     {
         // Arrange
         var lambdaContext = new TestLambdaContext 
         {
             AwsRequestId = Guid.NewGuid().ToString(),
             FunctionName = Guid.NewGuid().ToString(),
             FunctionVersion = Guid.NewGuid().ToString(),
             InvokedFunctionArn = Guid.NewGuid().ToString(),
             LogGroupName = Guid.NewGuid().ToString(),
             LogStreamName = Guid.NewGuid().ToString(),
             MemoryLimitInMB = new Random().Next()
         };
         
         var args = Substitute.For<AspectEventArgs>();
         var method = Substitute.For<MethodInfo>();
         var parameter = Substitute.For<ParameterInfo>();
         
         // Setup parameter
         parameter.ParameterType.Returns(typeof(ILambdaContext));

         // Setup method
         method.GetParameters().Returns(new[] { parameter });

         // Setup args
         args.Method = method;
         args.Args = new object[] { lambdaContext }; 
         
         // Act && Assert
         LoggingLambdaContext.Clear();
         Assert.Null(LoggingLambdaContext.Instance);
         Assert.True(LoggingLambdaContext.Extract(args));
         Assert.NotNull(LoggingLambdaContext.Instance);
         Assert.Equal(LoggingLambdaContext.Instance.AwsRequestId, lambdaContext.AwsRequestId);
         Assert.Equal(LoggingLambdaContext.Instance.FunctionName, lambdaContext.FunctionName);
         Assert.Equal(LoggingLambdaContext.Instance.FunctionVersion, lambdaContext.FunctionVersion);
         Assert.Equal(LoggingLambdaContext.Instance.InvokedFunctionArn, lambdaContext.InvokedFunctionArn);
         Assert.Equal(LoggingLambdaContext.Instance.LogGroupName, lambdaContext.LogGroupName);
         Assert.Equal(LoggingLambdaContext.Instance.LogStreamName, lambdaContext.LogStreamName);
         Assert.Equal(LoggingLambdaContext.Instance.MemoryLimitInMB, lambdaContext.MemoryLimitInMB);
         LoggingLambdaContext.Clear();
         Assert.Null(LoggingLambdaContext.Instance);
     }
     
     [Fact]
     public void Extract_When_LambdaContext_Is_Null_But_Not_First_Parameter_Returns_False()
     {
         // Arrange
         ILambdaContext lambdaContext = null;
         var args = Substitute.For<AspectEventArgs>();
         var method = Substitute.For<MethodInfo>();
         var parameter1 = Substitute.For<ParameterInfo>();
         var parameter2 = Substitute.For<ParameterInfo>();
         
         // Setup parameters
         parameter1.ParameterType.Returns(typeof(string));
         parameter2.ParameterType.Returns(typeof(ILambdaContext));

         // Setup method
         method.GetParameters().Returns(new[] { parameter1, parameter2 });

         // Setup args
         args.Method = method;
         args.Args = new object[] { "requestContext", lambdaContext }; 
         
         // Act && Assert
         LoggingLambdaContext.Clear();
         Assert.Null(LoggingLambdaContext.Instance);
         Assert.False(LoggingLambdaContext.Extract(args));
     }
     
     [Fact]
     public void Extract_When_Args_Null_Returns_False()
     {
         // Arrange
         var args = Substitute.For<AspectEventArgs>(); 
         
         // Act && Assert
         LoggingLambdaContext.Clear();
         Assert.Null(LoggingLambdaContext.Instance);
         Assert.False(LoggingLambdaContext.Extract(args));
     }
     
     [Fact]
     public void Extract_When_Method_Null_Returns_False()
     {
         // Arrange
         var args = Substitute.For<AspectEventArgs>(); 
         args.Args = Array.Empty<object>(); 
         
         // Act && Assert
         LoggingLambdaContext.Clear();
         Assert.Null(LoggingLambdaContext.Instance);
         Assert.False(LoggingLambdaContext.Extract(args));
     }
     
     [Fact]
     public void Extract_WhenInstance_Already_Created_Returns_False()
     {
         // Arrange
         var lambdaContext = new TestLambdaContext 
         {
             AwsRequestId = Guid.NewGuid().ToString(),
             FunctionName = Guid.NewGuid().ToString(),
             FunctionVersion = Guid.NewGuid().ToString(),
             InvokedFunctionArn = Guid.NewGuid().ToString(),
             LogGroupName = Guid.NewGuid().ToString(),
             LogStreamName = Guid.NewGuid().ToString(),
             MemoryLimitInMB = new Random().Next()
         };
         
         var args = Substitute.For<AspectEventArgs>();
         var method = Substitute.For<MethodInfo>();
         var parameter = Substitute.For<ParameterInfo>();
         
         // Setup parameter
         parameter.ParameterType.Returns(typeof(ILambdaContext));

         // Setup method
         method.GetParameters().Returns(new[] { parameter });

         // Setup args
         args.Method = method;
         args.Args = new object[] { lambdaContext }; 
         
         // Act && Assert
         LoggingLambdaContext.Clear();
         Assert.Null(LoggingLambdaContext.Instance);
         Assert.True(LoggingLambdaContext.Extract(args));
         Assert.NotNull(LoggingLambdaContext.Instance);
         Assert.False(LoggingLambdaContext.Extract(args));
     }
}
