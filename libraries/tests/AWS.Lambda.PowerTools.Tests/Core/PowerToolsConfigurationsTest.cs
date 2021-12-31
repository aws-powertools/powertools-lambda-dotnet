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
using AWS.Lambda.PowerTools.Core;
using Moq;
using Xunit;

namespace AWS.Lambda.PowerTools.Tests.Core
{
    public class PowerToolsConfigurationsTest
    {
        #region GetEnvironmentVariable Tests
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableIsNull_ReturnsDefaultValueString()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var defaultValue = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(key)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, defaultValue);

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == key)
                ), Times.Once);
            
            Assert.Equal(result, defaultValue);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableIsNull_ReturnsDefaultValueFalse()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(key)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, false);

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == key)
                ), Times.Once);
            
            Assert.False(result);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableIsNull_ReturnsDefaultValueTrue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(key)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, true);

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == key)
                ), Times.Once);
            
            Assert.True(result);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableHasValue_ReturnsValueString()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var defaultValue = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(key)
            ).Returns(value);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, defaultValue);

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == key)
                ), Times.Once);
            
            Assert.Equal(result, value);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableHasValue_ReturnsValueTrue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(key)
            ).Returns("true");
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, false);

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == key)
                ), Times.Once);
            
            Assert.True(result);
        }
        
        [Fact]
        public void GetEnvironmentVariableOrDefault_WhenEnvironmentVariableHasValue_ReturnsValueFalse()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(key)
            ).Returns("false");
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.GetEnvironmentVariableOrDefault(key, true);

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == key)
                ), Times.Once);
            
            Assert.False(result);
        }
        
        #endregion

        #region Service Tests

        [Fact]
        public void Service_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var defaultService = "service_undefined";
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SERVICE_NAME_ENV)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.Service;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SERVICE_NAME_ENV)
                ), Times.Once);
            
            Assert.Equal(result, defaultService);
        }
        
        [Fact]
        public void Service_WhenEnvironmentHasValue_ReturnsValue()
        {
            // Arrange
            var service = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SERVICE_NAME_ENV)
            ).Returns(service);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.Service;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SERVICE_NAME_ENV)
                ), Times.Once);
            
            Assert.Equal(result, service);
        }

        #endregion
        
        #region IsServiceDefined Tests
        
        [Fact]
        public void IsServiceDefined_WhenEnvironmentHasValue_ReturnsTrue()
        {
            // Arrange
            var service = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SERVICE_NAME_ENV)
            ).Returns(service);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.IsServiceDefined;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SERVICE_NAME_ENV)
                ), Times.Once);
            
            Assert.True(result);
        }
        
        [Fact]
        public void IsServiceDefined_WhenEnvironmentDoesNotHaveValue_ReturnsFalse()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SERVICE_NAME_ENV)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.IsServiceDefined;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SERVICE_NAME_ENV)
                ), Times.Once);
            
            Assert.False(result);
        }
        
        #endregion
        
        #region TracerCaptureResponse Tests

        [Fact]
        public void TracerCaptureResponse_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.TRACER_CAPTURE_RESPONSE_ENV)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.TracerCaptureResponse;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.TRACER_CAPTURE_RESPONSE_ENV)
                ), Times.Once);
            
            Assert.True(result);
        }
        
        [Fact]
        public void TracerCaptureResponse_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.TRACER_CAPTURE_RESPONSE_ENV)
            ).Returns("false");
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.TracerCaptureResponse;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.TRACER_CAPTURE_RESPONSE_ENV)
                ), Times.Once);
            
            Assert.False(result);
        }
        
        [Fact]
        public void TracerCaptureResponse_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.TRACER_CAPTURE_RESPONSE_ENV)
            ).Returns("true");
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.TracerCaptureResponse;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.TRACER_CAPTURE_RESPONSE_ENV)
                ), Times.Once);
            
            Assert.True(result);
        }
        
        #endregion
        
        #region TracerCaptureError Tests

        [Fact]
        public void TracerCaptureError_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.TRACER_CAPTURE_ERROR_ENV)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.TracerCaptureError;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.TRACER_CAPTURE_ERROR_ENV)
                ), Times.Once);
            
            Assert.True(result);
        }
        
        [Fact]
        public void TracerCaptureError_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.TRACER_CAPTURE_ERROR_ENV)
            ).Returns("false");
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.TracerCaptureError;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.TRACER_CAPTURE_ERROR_ENV)
                ), Times.Once);
            
            Assert.False(result);
        }
        
        [Fact]
        public void TracerCaptureError_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.TRACER_CAPTURE_ERROR_ENV)
            ).Returns("true");
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.TracerCaptureError;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.TRACER_CAPTURE_ERROR_ENV)
                ), Times.Once);
            
            Assert.True(result);
        }
        
        #endregion
        
        #region TracerCaptureError Tests

        [Fact]
        public void IsSamLocal_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SAM_LOCAL_ENV)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.IsSamLocal;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SAM_LOCAL_ENV)
                ), Times.Once);
            
            Assert.False(result);
        }
        
        [Fact]
        public void IsSamLocal_WhenEnvironmentHasValue_ReturnsValueFalse()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SAM_LOCAL_ENV)
            ).Returns("false");
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.IsSamLocal;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SAM_LOCAL_ENV)
                ), Times.Once);
            
            Assert.False(result);
        }
        
        [Fact]
        public void IsSamLocal_WhenEnvironmentHasValue_ReturnsValueTrue()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SAM_LOCAL_ENV)
            ).Returns("true");
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.IsSamLocal;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SAM_LOCAL_ENV)
                ), Times.Once);
            
            Assert.True(result);
        }
        
        #endregion
    }
}