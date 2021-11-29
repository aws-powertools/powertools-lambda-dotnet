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

        #region ServiceName Tests

        [Fact]
        public void ServiceName_WhenEnvironmentIsNull_ReturnsDefaultValue()
        {
            // Arrange
            var defaultServiceName = "service_undefined";
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SERVICE_NAME_ENV)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.ServiceName;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SERVICE_NAME_ENV)
                ), Times.Once);
            
            Assert.Equal(result, defaultServiceName);
        }
        
        [Fact]
        public void ServiceName_WhenEnvironmentHasValue_ReturnsValue()
        {
            // Arrange
            var serviceName = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SERVICE_NAME_ENV)
            ).Returns(serviceName);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.ServiceName;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SERVICE_NAME_ENV)
                ), Times.Once);
            
            Assert.Equal(result, serviceName);
        }

        #endregion
        
        #region IsServiceNameDefined Tests
        
        [Fact]
        public void IsServiceNameDefined_WhenEnvironmentHasValue_ReturnsTrue()
        {
            // Arrange
            var serviceName = Guid.NewGuid().ToString();
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SERVICE_NAME_ENV)
            ).Returns(serviceName);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.IsServiceNameDefined;

            // Assert
            systemWrapper.Verify(v =>
                v.GetEnvironmentVariable(
                    It.Is<string>(i => i == Constants.SERVICE_NAME_ENV)
                ), Times.Once);
            
            Assert.True(result);
        }
        
        [Fact]
        public void IsServiceNameDefined_WhenEnvironmentDoesNotHaveValue_ReturnsFalse()
        {
            // Arrange
            var systemWrapper = new Mock<ISystemWrapper>();

            systemWrapper.Setup(c =>
                c.GetEnvironmentVariable(Constants.SERVICE_NAME_ENV)
            ).Returns(string.Empty);
            
            var configurations = new PowerToolsConfigurations(systemWrapper.Object);
            
            // Act
            var result = configurations.IsServiceNameDefined;

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