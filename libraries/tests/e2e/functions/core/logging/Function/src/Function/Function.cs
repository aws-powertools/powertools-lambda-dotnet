using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;
using Helpers;
using Microsoft.Extensions.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Function
{
    public class Function
    {
        [Logging(LogEvent = true, LoggerOutputCase = LoggerOutputCase.PascalCase, Service = "TestService",
            CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
        {
            TestHelper.TestMethod(apigwProxyEvent);

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Body = apigwProxyEvent.Body.ToUpper()
            };
        }
    }
}

namespace StaticConfiguration
{
    public class Function
    {
        public Function()
        {
            Logger.Configure(config =>
            {
                config.Service = "TestService";
                config.LoggerOutputCase = LoggerOutputCase.PascalCase;
            });
        }

        [Logging(LogEvent = true, CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
        {
            TestHelper.TestMethod(apigwProxyEvent);

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Body = apigwProxyEvent.Body.ToUpper()
            };
        }
    }
}

namespace StaticILoggerConfiguration
{
    public class Function
    {
        public Function()
        {
            LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "TestService";
                    config.LoggerOutputCase = LoggerOutputCase.PascalCase;
                });
            });
        }

        [Logging(LogEvent = true, CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
        {
            TestHelper.TestMethod(apigwProxyEvent);

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Body = apigwProxyEvent.Body.ToUpper()
            };
        }
    }
}

namespace ILoggerConfiguration
{
    public class Function
    {
        private readonly ILogger _logger;

        public Function()
        {
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "TestService";
                    config.LoggerOutputCase = LoggerOutputCase.PascalCase;
                });
            }).CreatePowertoolsLogger();
        }

        [Logging(LogEvent = true, CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
        {
            _logger.LogInformation("Processing request started");
        
            var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;
            var lookupInfo = new Dictionary<string, object>()
            {
                {"LookupInfo", new Dictionary<string, object>{{ "LookupId", requestContextRequestId }}}
            };  
        
            var customKeys = new Dictionary<string, string>
            {
                {"test1", "value1"}, 
                {"test2", "value2"}
            };
        
            _logger.AppendKeys(lookupInfo);
            _logger.AppendKeys(customKeys);
        
            _logger.LogWarning("Warn with additional keys");
        
            _logger.RemoveKeys("test1", "test2");
        
            var error = new InvalidOperationException("Parent exception message",
                new ArgumentNullException(nameof(apigwProxyEvent),
                    new Exception("Very important nested inner exception message")));
            _logger.LogError(error, "Oops something went wrong");

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Body = apigwProxyEvent.Body.ToUpper()
            };
        }
    }
}

namespace ILoggerBuilder
{
    public class Function
    {
        private readonly ILogger _logger;

        public Function()
        {
            _logger = new PowertoolsLoggerBuilder()
                .WithService("TestService")
                .WithOutputCase(LoggerOutputCase.PascalCase)
                .Build();
        }

        [Logging(LogEvent = true, CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigwProxyEvent, ILambdaContext context)
        {
            _logger.LogInformation("Processing request started");
        
            var requestContextRequestId = apigwProxyEvent.RequestContext.RequestId;
            var lookupInfo = new Dictionary<string, object>()
            {
                {"LookupInfo", new Dictionary<string, object>{{ "LookupId", requestContextRequestId }}}
            };  
        
            var customKeys = new Dictionary<string, string>
            {
                {"test1", "value1"}, 
                {"test2", "value2"}
            };
        
            _logger.AppendKeys(lookupInfo);
            _logger.AppendKeys(customKeys);
        
            _logger.LogWarning("Warn with additional keys");
        
            _logger.RemoveKeys("test1", "test2");
        
            var error = new InvalidOperationException("Parent exception message",
                new ArgumentNullException(nameof(apigwProxyEvent),
                    new Exception("Very important nested inner exception message")));
            _logger.LogError(error, "Oops something went wrong");

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Body = apigwProxyEvent.Body.ToUpper()
            };
        }
    }
}