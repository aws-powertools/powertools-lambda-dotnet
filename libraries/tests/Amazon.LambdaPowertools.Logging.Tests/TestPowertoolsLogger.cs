using System;
using System.IO;
using Amazon.LambdaPowertools.Logging;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Amazon.LambdaPowertools.Logging.Tests
{
    public class TestPowertoolsLogger
    {
        private ILogger _logger;
        private readonly ITestOutputHelper _testOutput;
        public TestPowertoolsLogger(ITestOutputHelper testOutput)
        {
            LoggerOptions loggerOptions = new LoggerOptions()
            {
                LogLevel = LogLevel.Information
            };
            _logger = new PowertoolsLogger(loggerOptions);
            _testOutput = testOutput;
        }
        
        

        


        [Fact]
        public void TestLogInformationOutput()
        {
            var output = new StringWriter();
            Console.SetOut(output);
            _logger.LogInformation("TEST");
            
            _testOutput.WriteLine(output.ToString());
            Assert.Contains("\"Message\":\"TEST\"", output.ToString());
        }
        
        [Fact]
        public void TestLogDebugOutputShouldFail()
        {
            var output = new StringWriter();
            Console.SetOut(output);
            _logger.LogDebug("TEST");
            
            _testOutput.WriteLine(output.ToString());
            Assert.Empty(output.ToString());
        }
            
        
    }
}