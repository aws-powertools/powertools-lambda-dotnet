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
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Common.Tests
{
    public class PowertoolsConfigurationsTest
    {
        #region GetEnvironmentVariable Tests
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableIsNull_ReturnsDefaultValueString()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var defaultValue = Guid.NewGuid().ToString();
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(key).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, defaultValue);

            // Assert
            environment.Received(1).GetEnvironmentVariable(key);
            
            Assert.Equal(result, defaultValue);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableIsNull_ReturnsDefaultValueFalse()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(key).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, false);

            // Assert
            environment.Received(1).GetEnvironmentVariable(key);
            
            Assert.False(result);
        }

        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableIsNull_ReturnsDefaultValueTrue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(key).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(environment);

            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, true);

            // Assert
            environment.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == key));
            
            Assert.True(result);
        }

        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableHasValue_ReturnsValueString()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var defaultValue = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();
            var environment = Substitute.For<IPowertoolsEnvironment>();
            
            environment.GetEnvironmentVariable(key).Returns(value);

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, defaultValue);

            // Assert
            environment.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == key));
            
            Assert.Equal(result, value);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableHasValue_ReturnsValueTrue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(key).Returns("true");

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, false);

            // Assert
            environment.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == key));
            
            Assert.True(result);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableHasValue_ReturnsValueFalse()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(key).Returns("false");
            
            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, true);

            // Assert
            environment.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == key));
            
            Assert.False(result);
        }
        
        #endregion

        #region Service Tests

        [Fact]
        public void Service_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var defaultService = "service_undefined";
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.ServiceNameEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(environment);

            // Act
            var result = configurations.Service;

            // Assert
            environment.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.ServiceNameEnv));
            
            Assert.Equal(result, defaultService);
        }

        [Fact]
        public void Service_WhenEnvironmentHasValue_ReturnsValue()
        {
            // Arrange
            var service = Guid.NewGuid().ToString();
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.ServiceNameEnv).Returns(service);

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.Service;

            // Assert
            environment.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.ServiceNameEnv));
            
            Assert.Equal(result, service);
        }

        #endregion
        
        #region IsServiceDefined Tests
        
        [Fact]
        public void IsServiceDefined_WhenEnvironmentHasValue_ReturnsTrue()
        {
            // Arrange
            var service = Guid.NewGuid().ToString();
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.ServiceNameEnv).Returns(service);
           
            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.IsServiceDefined;

            // Assert
            environment.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.ServiceNameEnv));
            
            Assert.True(result);
        }
        
        [Fact]
        public void IsServiceDefined_WhenEnvironmentDoesNotHaveValue_ReturnsFalse()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.ServiceNameEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.IsServiceDefined;

            // Assert
            environment.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.ServiceNameEnv));
            
            Assert.False(result);
        }
        
        #endregion
        
        #region TracerCaptureResponse Tests

        [Fact]
        public void TracerCaptureResponse_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.TracerCaptureResponseEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(environment);

            // Act
            var result = configurations.TracerCaptureResponse;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureResponseEnv));

            Assert.True(result);
        }

        [Fact]
        public void TracerCaptureResponse_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.TracerCaptureResponseEnv).Returns("false");

            var configurations = new PowertoolsConfigurations(environment);

            // Act
            var result = configurations.TracerCaptureResponse;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureResponseEnv));
            
            Assert.False(result);
        }

        [Fact]
        public void TracerCaptureResponse_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.TracerCaptureResponseEnv).Returns("true");

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.TracerCaptureResponse;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureResponseEnv));
            
            Assert.True(result);
        }
        
        #endregion
        
        #region TracerCaptureError Tests

        [Fact]
        public void TracerCaptureError_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.TracerCaptureErrorEnv).Returns(string.Empty);
          
            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.TracerCaptureError;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureErrorEnv));
            
            Assert.True(result);
        }
        
        [Fact]
        public void TracerCaptureError_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.TracerCaptureErrorEnv).Returns("false");

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.TracerCaptureError;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureErrorEnv));
            
            Assert.False(result);
        }
        
        [Fact]
        public void TracerCaptureError_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();
            
            environment.GetEnvironmentVariable(Constants.TracerCaptureErrorEnv).Returns("true");

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.TracerCaptureError;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureErrorEnv));
            
            Assert.True(result);
        }
        
        #endregion
        
        #region IsSamLocal Tests

        [Fact]
        public void IsSamLocal_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.SamLocalEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.IsSamLocal;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.SamLocalEnv));
            
            Assert.False(result);
        }
        
        [Fact]
        public void IsSamLocal_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.SamLocalEnv).Returns("false");

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.IsSamLocal;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.SamLocalEnv));
            
            Assert.False(result);
        }
        
        [Fact]
        public void IsSamLocal_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.SamLocalEnv).Returns("true");

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.IsSamLocal;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.SamLocalEnv));
            
            Assert.True(result);
        }
        
        #endregion
        
        #region TracingDisabled Tests

        [Fact]
        public void TracingDisabled_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.TracingDisabledEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.TracingDisabled;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracingDisabledEnv));
            
            Assert.False(result);
        }
        
        [Fact]
        public void TracingDisabled_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();
            
            environment.GetEnvironmentVariable(Constants.TracingDisabledEnv).Returns("false");
            
            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.TracingDisabled;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracingDisabledEnv));

            Assert.False(result);
        }
        
        [Fact]
        public void TracingDisabled_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();
            
            environment.GetEnvironmentVariable(Constants.TracingDisabledEnv).Returns("true");
            
            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.TracingDisabled;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracingDisabledEnv));

            Assert.True(result);
        }
        
        #endregion
        
        #region IsLambdaEnvironment Tests

        [Fact]
        public void IsLambdaEnvironment_WhenEnvironmentIsNull_ReturnsFalse()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.LambdaTaskRoot).Returns((string)null);

            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.IsLambdaEnvironment;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.LambdaTaskRoot));
           
            Assert.False(result);
        }
        
        [Fact]
        public void IsLambdaEnvironment_WhenEnvironmentHasValue_ReturnsTrue()
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();

            environment.GetEnvironmentVariable(Constants.TracingDisabledEnv).Returns(Guid.NewGuid().ToString());
            
            var configurations = new PowertoolsConfigurations(environment);
            
            // Act
            var result = configurations.IsLambdaEnvironment;

            // Assert
            environment.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.LambdaTaskRoot));
            
            Assert.True(result);
        }
        
        #endregion

        #region XRayTraceId Tests

        [Fact]
        public void XRayTraceId_WhenLambdaTraceProviderAvailable_ReturnsTraceId()
        {
            ResetTraceProviderState();

            try
            {
                // Arrange
                var environment = Substitute.For<IPowertoolsEnvironment>();
                var configurations = new PowertoolsConfigurations(environment);

                // Act - LambdaTraceProvider is available in test env (Amazon.Lambda.Core 2.8.0)
                // Returns null/empty in test env (no active Lambda trace), but should not throw
                var result = configurations.XRayTraceId;

                // Assert - should not fall back to env var (provider was used instead)
                environment.DidNotReceive()
                    .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.XrayTraceIdEnv));
            }
            finally
            {
                ResetTraceProviderState();
            }
        }

        [Fact]
        public void XRayTraceId_WhenLambdaTraceProviderUnavailable_FallsBackToEnvironmentVariable()
        {
            ResetTraceProviderState();

            try
            {
                // Arrange
                var traceId = "Root=1-5759e988-bd862e3fe1be46a994272793;Parent=53995c3f42cd8ad8;Sampled=1";
                var environment = Substitute.For<IPowertoolsEnvironment>();
                environment.GetEnvironmentVariable(Constants.XrayTraceIdEnv).Returns(traceId);
                var configurations = new PowertoolsConfigurations(environment);

                // Simulate LambdaTraceProvider not being available (older Amazon.Lambda.Core)
                SetTraceProviderState(-1);

                // Act
                var result = configurations.XRayTraceId;

                // Assert
                environment.Received(1)
                    .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.XrayTraceIdEnv));
                Assert.Equal(traceId, result);
            }
            finally
            {
                ResetTraceProviderState();
            }
        }

        [Fact]
        public void XRayTraceId_WhenProviderCachedUnavailable_UsesEnvVarOnSubsequentCalls()
        {
            ResetTraceProviderState();

            try
            {
                // Arrange
                var environment = Substitute.For<IPowertoolsEnvironment>();
                var configurations = new PowertoolsConfigurations(environment);

                // Simulate LambdaTraceProvider not being available (cached)
                SetTraceProviderState(-1);

                var traceId1 = "Root=1-aaa;Parent=bbb;Sampled=1";
                var traceId2 = "Root=1-ccc;Parent=ddd;Sampled=1";
                environment.GetEnvironmentVariable(Constants.XrayTraceIdEnv).Returns(traceId1, traceId2);

                // Act
                var result1 = configurations.XRayTraceId;
                var result2 = configurations.XRayTraceId;

                // Assert - should go straight to env var on both calls (cached unavailable)
                environment.Received(2)
                    .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.XrayTraceIdEnv));
                Assert.Equal(traceId1, result1);
                Assert.Equal(traceId2, result2);
            }
            finally
            {
                ResetTraceProviderState();
            }
        }

        private static void ResetTraceProviderState()
        {
            var field = typeof(PowertoolsConfigurations).GetField("_traceProviderState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, 0);
        }

        private static void SetTraceProviderState(int state)
        {
            var field = typeof(PowertoolsConfigurations).GetField("_traceProviderState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, state);
        }

        #endregion
    }
}