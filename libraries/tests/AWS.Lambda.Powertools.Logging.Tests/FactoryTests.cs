using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests;

public class LoggingAspectFactoryTests
{
    [Fact]
    public void GetInstance_ShouldReturnLoggingAspectInstance()
    {
        // Act
        var result = LoggingAspectFactory.GetInstance(typeof(LoggingAspectFactoryTests));
            
        // Assert
        Assert.NotNull(result);
        Assert.IsType<LoggingAspect>(result);
    }
}

public class PowertoolsLoggerFactoryTests
    {
        [Fact]
        public void Constructor_WithLoggerFactory_CreatesPowertoolsLoggerFactory()
        {
            // Arrange
            var mockFactory = Substitute.For<ILoggerFactory>();
            
            // Act
            var factory = new PowertoolsLoggerFactory(mockFactory);
            
            // Assert
            Assert.NotNull(factory);
        }
        
        [Fact]
        public void DefaultConstructor_CreatesPowertoolsLoggerFactory()
        {
            // Act
            var factory = new PowertoolsLoggerFactory();
            
            // Assert
            Assert.NotNull(factory);
        }
        
        [Fact]
        public void Create_WithConfigAction_ReturnsPowertoolsLoggerFactory()
        {
            // Act
            var factory = PowertoolsLoggerFactory.Create(options => 
            {
                options.Service = "TestService";
            });
            
            // Assert
            Assert.NotNull(factory);
        }
        
        [Fact]
        public void Create_WithConfiguration_ReturnsLoggerFactory()
        {
            // Arrange
            var configuration = new PowertoolsLoggerConfiguration
            {
                Service = "TestService"
            };
            
            // Act
            var factory = PowertoolsLoggerFactory.Create(configuration);
            
            // Assert
            Assert.NotNull(factory);
        }
        
        [Fact]
        public void CreateBuilder_ReturnsLoggerBuilder()
        {
            // Act
            var builder = PowertoolsLoggerFactory.CreateBuilder();
            
            // Assert
            Assert.NotNull(builder);
            Assert.IsType<PowertoolsLoggerBuilder>(builder);
        }
        
        [Fact]
        public void CreateLogger_Generic_ReturnsLogger()
        {
            // Arrange
            var mockFactory = Substitute.For<ILoggerFactory>();
            mockFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
            var factory = new PowertoolsLoggerFactory(mockFactory);
            
            // Act
            var logger = factory.CreateLogger<PowertoolsLoggerFactoryTests>();
            
            // Assert
            Assert.NotNull(logger);
            mockFactory.Received(1).CreateLogger(typeof(PowertoolsLoggerFactoryTests).FullName);
        }
        
        [Fact]
        public void CreateLogger_WithCategory_ReturnsLogger()
        {
            // Arrange
            var mockFactory = Substitute.For<ILoggerFactory>();
            mockFactory.CreateLogger("TestCategory").Returns(Substitute.For<ILogger>());
            var factory = new PowertoolsLoggerFactory(mockFactory);
            
            // Act
            var logger = factory.CreateLogger("TestCategory");
            
            // Assert
            Assert.NotNull(logger);
            mockFactory.Received(1).CreateLogger("TestCategory");
        }
        
        [Fact]
        public void CreatePowertoolsLogger_ReturnsPowertoolsLogger()
        {
            // Arrange
            var mockFactory = Substitute.For<ILoggerFactory>();
            mockFactory.CreatePowertoolsLogger().Returns(Substitute.For<ILogger>());
            var factory = new PowertoolsLoggerFactory(mockFactory);
            
            // Act
            var logger = factory.CreatePowertoolsLogger();
            
            // Assert
            Assert.NotNull(logger);
            mockFactory.Received(1).CreatePowertoolsLogger();
        }
        
        [Fact]
        public void Dispose_DisposesInnerFactory()
        {
            // Arrange
            var mockFactory = Substitute.For<ILoggerFactory>();
            var factory = new PowertoolsLoggerFactory(mockFactory);
            
            // Act
            factory.Dispose();
            
            // Assert
            mockFactory.Received(1).Dispose();
        }
    }