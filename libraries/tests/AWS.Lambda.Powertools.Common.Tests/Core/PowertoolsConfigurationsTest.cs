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
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(key).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, defaultValue);

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(key);
            
            Assert.Equal(result, defaultValue);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableIsNull_ReturnsDefaultValueFalse()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(key).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, false);

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(key);
            
            Assert.False(result);
        }

        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableIsNull_ReturnsDefaultValueTrue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(key).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(systemWrapper);

            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, true);

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == key));
            
            Assert.True(result);
        }

        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableHasValue_ReturnsValueString()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var defaultValue = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();
            var systemWrapper = Substitute.For<ISystemWrapper>();
            
            systemWrapper.GetEnvironmentVariable(key).Returns(value);

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, defaultValue);

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == key));
            
            Assert.Equal(result, value);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableHasValue_ReturnsValueTrue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(key).Returns("true");

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, false);

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == key));
            
            Assert.True(result);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableHasValue_ReturnsValueFalse()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(key).Returns("false");
            
            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, true);

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == key));
            
            Assert.False(result);
        }
        
        #endregion

        #region Service Tests

        [Fact]
        public void Service_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var defaultService = "service_undefined";
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.ServiceNameEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(systemWrapper);

            // Act
            var result = configurations.Service;

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.ServiceNameEnv));
            
            Assert.Equal(result, defaultService);
        }

        [Fact]
        public void Service_WhenEnvironmentHasValue_ReturnsValue()
        {
            // Arrange
            var service = Guid.NewGuid().ToString();
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.ServiceNameEnv).Returns(service);

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.Service;

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.ServiceNameEnv));
            
            Assert.Equal(result, service);
        }

        #endregion
        
        #region IsServiceDefined Tests
        
        [Fact]
        public void IsServiceDefined_WhenEnvironmentHasValue_ReturnsTrue()
        {
            // Arrange
            var service = Guid.NewGuid().ToString();
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.ServiceNameEnv).Returns(service);
           
            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.IsServiceDefined;

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.ServiceNameEnv));
            
            Assert.True(result);
        }
        
        [Fact]
        public void IsServiceDefined_WhenEnvironmentDoesNotHaveValue_ReturnsFalse()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.ServiceNameEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.IsServiceDefined;

            // Assert
            systemWrapper.Received(1).GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.ServiceNameEnv));
            
            Assert.False(result);
        }
        
        #endregion
        
        #region TracerCaptureResponse Tests

        [Fact]
        public void TracerCaptureResponse_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.TracerCaptureResponseEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(systemWrapper);

            // Act
            var result = configurations.TracerCaptureResponse;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureResponseEnv));

            Assert.True(result);
        }

        [Fact]
        public void TracerCaptureResponse_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.TracerCaptureResponseEnv).Returns("false");

            var configurations = new PowertoolsConfigurations(systemWrapper);

            // Act
            var result = configurations.TracerCaptureResponse;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureResponseEnv));
            
            Assert.False(result);
        }

        [Fact]
        public void TracerCaptureResponse_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.TracerCaptureResponseEnv).Returns("true");

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.TracerCaptureResponse;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureResponseEnv));
            
            Assert.True(result);
        }
        
        #endregion
        
        #region TracerCaptureError Tests

        [Fact]
        public void TracerCaptureError_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.TracerCaptureErrorEnv).Returns(string.Empty);
          
            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.TracerCaptureError;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureErrorEnv));
            
            Assert.True(result);
        }
        
        [Fact]
        public void TracerCaptureError_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.TracerCaptureErrorEnv).Returns("false");

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.TracerCaptureError;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureErrorEnv));
            
            Assert.False(result);
        }
        
        [Fact]
        public void TracerCaptureError_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();
            
            systemWrapper.GetEnvironmentVariable(Constants.TracerCaptureErrorEnv).Returns("true");

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.TracerCaptureError;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracerCaptureErrorEnv));
            
            Assert.True(result);
        }
        
        #endregion
        
        #region IsSamLocal Tests

        [Fact]
        public void IsSamLocal_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.SamLocalEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.IsSamLocal;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.SamLocalEnv));
            
            Assert.False(result);
        }
        
        [Fact]
        public void IsSamLocal_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.SamLocalEnv).Returns("false");

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.IsSamLocal;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.SamLocalEnv));
            
            Assert.False(result);
        }
        
        [Fact]
        public void IsSamLocal_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.SamLocalEnv).Returns("true");

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.IsSamLocal;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.SamLocalEnv));
            
            Assert.True(result);
        }
        
        #endregion
        
        #region TracingDisabled Tests

        [Fact]
        public void TracingDisabled_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.TracingDisabledEnv).Returns(string.Empty);

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.TracingDisabled;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracingDisabledEnv));
            
            Assert.False(result);
        }
        
        [Fact]
        public void TracingDisabled_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();
            
            systemWrapper.GetEnvironmentVariable(Constants.TracingDisabledEnv).Returns("false");
            
            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.TracingDisabled;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracingDisabledEnv));

            Assert.False(result);
        }
        
        [Fact]
        public void TracingDisabled_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();
            
            systemWrapper.GetEnvironmentVariable(Constants.TracingDisabledEnv).Returns("true");
            
            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.TracingDisabled;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.TracingDisabledEnv));

            Assert.True(result);
        }
        
        #endregion
        
        #region IsLambdaEnvironment Tests

        [Fact]
        public void IsLambdaEnvironment_WhenEnvironmentIsNull_ReturnsFalse()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.LambdaTaskRoot).Returns((string)null);

            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.IsLambdaEnvironment;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.LambdaTaskRoot));
           
            Assert.False(result);
        }
        
        [Fact]
        public void IsLambdaEnvironment_WhenEnvironmentHasValue_ReturnsTrue()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            systemWrapper.GetEnvironmentVariable(Constants.TracingDisabledEnv).Returns(Guid.NewGuid().ToString());
            
            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            var result = configurations.IsLambdaEnvironment;

            // Assert
            systemWrapper.Received(1)
                .GetEnvironmentVariable(Arg.Is<string>(i => i == Constants.LambdaTaskRoot));
            
            Assert.True(result);
        }
        
        [Fact]
        public void Set_Lambda_Execution_Context()
        {
            // Arrange
            var systemWrapper = Substitute.For<ISystemWrapper>();

            // systemWrapper.Setup(c =>
            //     c.SetExecutionEnvironment(GetType())
            // );
            
            var configurations = new PowertoolsConfigurations(systemWrapper);
            
            // Act
            configurations.SetExecutionEnvironment(typeof(PowertoolsConfigurations));

            // Assert
            // method with correct type was called
            systemWrapper.Received(1)
                .SetExecutionEnvironment(Arg.Is<Type>(i => i == typeof(PowertoolsConfigurations)));
        }
        
        #endregion
    }
}