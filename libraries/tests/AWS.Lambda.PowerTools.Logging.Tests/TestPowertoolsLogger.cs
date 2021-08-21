using System;
using System.IO;
using AWS.Lambda.PowerTools.Logging;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.PowerTools.Logging.Tests
{
    public class TestPowertoolsLogger
    {
        private ILogger _logger;
        private readonly ITestOutputHelper _testOutput;
        public TestPowertoolsLogger(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }
        
        [Fact]
        public void TestLogInformationOutput()
        {
            // Initialize
            var output = new StringWriter();
            Console.SetOut(output);
            Environment.SetEnvironmentVariable("LOG_LEVEL", "Information");
            
            var loggerOptions = new LoggerOptions();
            _logger = new PowertoolsLogger(loggerOptions);
            const string logInfo = "TEST INFO ??!?";
            const string logDebug = "TEST DEBUG ##11";
            
            // Assert
            _logger.LogInformation(logInfo);
            _logger.LogDebug(logDebug);
            
            _testOutput.WriteLine(output.ToString());
            Assert.Contains(logInfo, output.ToString());
            Assert.DoesNotContain(logDebug, output.ToString());
            
        }

        [Fact]
        public void TestLogDebugOutput()
        {
            // Initialize
            var output = new StringWriter();
            Console.SetOut(output);
            Environment.SetEnvironmentVariable("LOG_LEVEL", "Debug");
            
            var loggerOptions = new LoggerOptions();
            _logger = new PowertoolsLogger(loggerOptions);
            const string logInfo = "TEST INFO ??!?";
            const string logDebug = "TEST DEBUG ##11";
            const string logTrace = "TEST TRACE ##1875";
            
            // Assert
            _logger.LogInformation(logInfo);
            _logger.LogDebug(logDebug);
            _logger.LogTrace(logTrace);
            
            _testOutput.WriteLine(output.ToString());
            Assert.Contains(logInfo, output.ToString());
            Assert.Contains(logDebug, output.ToString());
            Assert.DoesNotContain(logTrace, output.ToString());
        }
        
        [Fact]
        public void TestLogLevelFromEnvironmentVariableMissing()
        {
            // Initialize
            var output = new StringWriter();
            Console.SetOut(output);
            Environment.SetEnvironmentVariable("LOG_LEVEL", null);
            //Environment.SetEnvironmentVariable("LOG_LEVEL", "Missing");

            var loggerOptions = new LoggerOptions();
            _logger = new PowertoolsLogger(loggerOptions);
            const string logInfo = "TEST INFO ??!?";
            const string logDebug = "TEST DEBUG ##11";
            
            // Assert
            _logger.LogInformation(logInfo);
            _logger.LogDebug(logDebug);
            
            _testOutput.WriteLine(output.ToString());
            Assert.Contains(logInfo, output.ToString());
            Assert.DoesNotContain(logDebug, output.ToString());
        }
        
        [Fact]
        public void TestLogLevelFromEnvironmentVariableTypo()
        {
            // Initialize
            var output = new StringWriter();
            Console.SetOut(output);
            Environment.SetEnvironmentVariable("LOG_LEVEL", "Typo");

            var loggerOptions = new LoggerOptions();
            _logger = new PowertoolsLogger(loggerOptions);
            const string logInfo = "TEST INFO ??!?";
            const string logDebug = "TEST DEBUG ##11";
            
            // Assert
            _logger.LogInformation(logInfo);
            _logger.LogDebug(logDebug);
            
            _testOutput.WriteLine(output.ToString());
            Assert.Contains(logInfo, output.ToString());
            Assert.DoesNotContain(logDebug, output.ToString());
        }
            
        
    }
}