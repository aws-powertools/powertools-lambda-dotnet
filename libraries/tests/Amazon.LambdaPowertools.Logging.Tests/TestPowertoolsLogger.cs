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
        private Logger _logger;
        private readonly ITestOutputHelper _testOutput;
        public TestPowertoolsLogger(ITestOutputHelper testOutput)
        {
            _logger = new Logger();
            _testOutput = testOutput;
        }
        
        

        


        [Fact]
        public void TestFunction()
        {
            var output = new StringWriter();
            Console.SetOut(output);
            //LogLevel level = LogLevel.Debug;
            _logger.Log("TEST");
            
            _testOutput.WriteLine(output.ToString());
            Assert.Contains("\"Message\":\"TEST\"", output.ToString());
        }
            
        
    }
}