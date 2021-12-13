---
title: Logging
description: Core utility
---

Logging provides an opinionated logger with output structured as JSON.

**Key features**

* Capture key fields from Lambda context, cold start and structures logging output as JSON
* Log Lambda event when instructed (disabled by default)
* Log sampling enables DEBUG log level for a percentage of requests (disabled by default)
* Append additional keys to structured log at any point in time

## Getting started

Logging requires two settings:

Setting | Description | Environment variable | Attribute parameter
------------------------------------------------- | ------------------------------------------------- | ------------------------------------------------- | -------------------------------------------------
**Logging level** | Sets how verbose Logger should be (Information, by default) |  `LOG_LEVEL` | `LogLevel`
**Service name** | Sets **Service** key that will be present across all log statements | `POWERTOOLS_SERVICE_NAME` | `ServiceName`

> Example using AWS Serverless Application Model (SAM)

You can also override log level by setting **`POWERTOOLS_LOG_LEVEL`** env var. Here is an example using AWS Serverless Application Model (SAM)

=== "template.yaml"
    ``` yaml hl_lines="9 10"
    Resources:
        HelloWorldFunction:
            Type: AWS::Serverless::Function
            Properties:
            ...
            Runtime: dotnetcore3.1
            Environment:
                Variables:
                    POWERTOOLS_LOG_LEVEL: Debug
                    POWERTOOLS_SERVICE_NAME: example
    ```

You can also explicitly set a service name via **`POWERTOOLS_SERVICE_NAME`** env var. This sets **service** key that will be present across all log statements.

## Standard structured keys

Your logs will always include the following keys to your structured logging:

Key | Type | Example | Description
------------------------------------------------- | ------------------------------------------------- | --------------------------------------------------------------------------------- | -------------------------------------------------
**Timestamp** | String | "2020-05-24 18:17:33,774" | Timestamp of actual log statement
**Level** | String | "Information" | Logging level
**Name** | String | "PowerTools Logger" | Logger name
**ColdStart** | Boolean | true| ColdStart value.
**Service** | String | "payment" | Service name defined. "service_undefined" will be used if unknown
**SamplingRate** | int |  0.1 | Debug logging sampling rate in percentage e.g. 10% in this case
**Message** | String |  "Collecting payment" | Log statement value. Unserializable JSON values will be casted to string
**FunctionName**| String | "example-powertools-HelloWorldFunction-1P1Z6B39FLU73"
**FunctionVersion**| String | "12"
**FunctionMemorySize**| String | "128"
**FunctionArn**| String | "arn:aws:lambda:eu-west-1:012345678910:function:example-powertools-HelloWorldFunction-1P1Z6B39FLU73"
**XRayTraceId**| String | "1-5759e988-bd862e3fe1be46a994272793" | X-Ray Trace ID when Lambda function has enabled Tracing
**FunctionRequestId**| String | "899856cb-83d1-40d7-8611-9e78f15f32f4" | AWS Request ID from lambda context
**State**| JSON | See below | JSON log entry

=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="7-11 16-19"
    {
		"ColdStart": true,
		"XRayTraceId": "1-61b600cf-5b6094cc123936136c4413e6",
		"FunctionName": "simple-lambda-test-HelloWorldFunction-6Riwvl7WcJt3",
		"FunctionVersion": "$LATEST",
		"FunctionMemorySize": 512,
		"FunctionArn": "arn:aws:lambda:eu-west-2:286043031651:function:simple-lambda-test-HelloWorldFunction-6Riwvl7WcJt3",
		"FunctionRequestId": "c3f11265-0a39-443c-814d-671a567f5fee",
		"Timestamp": "2021-12-12T14:01:53.4222470Z",
		"Level": "Information",
		"Service": "lambda-example",
		"Name": "AWS.Lambda.PowerTools.Logging.Logger",
		"Message": "hello world",
		"SamplingRate": 0.7,
		"State": [
			{
				"Key": "body",
				"Value": {
					"message": "hello world"
				}
			}
		]
	}
    ```

## Capturing context Lambda info

You can enrich your structured logs with key Lambda context information via `logEvent` annotation parameter. 
You can also explicitly log any incoming event using `logEvent` param. Refer [Override default object mapper](#override-default-object-mapper) 
to customise what is logged.

!!! warning
    Log event is disabled by default to prevent sensitive info being logged.


=== "App.java"

    ```java hl_lines="14"
    import org.apache.logging.log4j.LogManager;
    import org.apache.logging.log4j.Logger;
    import software.amazon.lambda.powertools.logging.LoggingUtils;
    import software.amazon.lambda.powertools.logging.Logging;
    ...
    
    /**
     * Handler for requests to Lambda function.
     */
    public class App implements RequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    
        Logger log = LogManager.getLogger();
    
        @Logging
        public APIGatewayProxyResponseEvent handleRequest(final APIGatewayProxyRequestEvent input, final Context context) {
         ...
        }
    }
    ```

=== "AppLogEvent.java"
    
    ```java hl_lines="8"
    /**
     * Handler for requests to Lambda function.
     */
    public class AppLogEvent implements RequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    
        Logger log = LogManager.getLogger();
        
        @Logging(logEvent = true)
        public APIGatewayProxyResponseEvent handleRequest(final APIGatewayProxyRequestEvent input, final Context context) {
         ...
        }
    }
    ```

## Setting a Correlation ID

You can set a Correlation ID using `correlationIdPath` attribute by passing a [JSON Pointer expression](https://datatracker.ietf.org/doc/html/draft-ietf-appsawg-json-pointer-03){target="_blank"}.

=== "App.java"

    ```java hl_lines="8"
    /**
     * Handler for requests to Lambda function.
     */
    public class App implements RequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    
        Logger log = LogManager.getLogger();
    
        @Logging(correlationIdPath = "/headers/my_request_id_header")
        public APIGatewayProxyResponseEvent handleRequest(final APIGatewayProxyRequestEvent input, final Context context) {
            ...
            log.info("Collecting payment")
            ...
        }
    }
    ```
=== "Example Event"

	```json hl_lines="3"
	{
	  "headers": {
		"my_request_id_header": "correlation_id_value"
	  }
	}
	```

=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="11"
	{
		"level": "INFO",
	  	"message": "Collecting payment",
		"timestamp": "2021-05-03 11:47:12,494+0200",
	  	"service": "payment",
	  	"coldStart": true,
	  	"functionName": "test",
	  	"functionMemorySize": 128,
	  	"functionArn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
	  	"lambda_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72",
	  	"correlation_id": "correlation_id_value"
	}
    ```
We provide [built-in JSON Pointer expression](https://datatracker.ietf.org/doc/html/draft-ietf-appsawg-json-pointer-03){target="_blank"} 
for known event sources, where either a request ID or X-Ray Trace ID are present.

=== "App.java"

    ```java hl_lines="10"
    import software.amazon.lambda.powertools.logging.CorrelationIdPathConstants;

    /**
     * Handler for requests to Lambda function.
     */
    public class App implements RequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    
        Logger log = LogManager.getLogger();
    
        @Logging(correlationIdPath = CorrelationIdPathConstants.API_GATEWAY_REST)
        public APIGatewayProxyResponseEvent handleRequest(final APIGatewayProxyRequestEvent input, final Context context) {
            ...
            log.info("Collecting payment")
            ...
        }
    }
    ```

=== "Example Event"

	```json hl_lines="3"
	{
	  "requestContext": {
		"requestId": "correlation_id_value"
	  }
	}
	```

=== "Example CloudWatch Logs excerpt"

    ```json hl_lines="11"
	{
		"level": "INFO",
	  	"message": "Collecting payment",
		"timestamp": "2021-05-03 11:47:12,494+0200",
	  	"service": "payment",
	  	"coldStart": true,
	  	"functionName": "test",
	  	"functionMemorySize": 128,
	  	"functionArn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
	  	"lambda_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72",
	  	"correlation_id": "correlation_id_value"
	}
    ```
	
## Appending additional keys

!!! info "Custom keys are persisted across warm invocations"
        Always set additional keys as part of your handler to ensure they have the latest value, or explicitly clear them with [`clearState=true`](#clearing-all-state).

You can append your own keys to your existing logs via `appendKey`.

=== "App.java"

    ```java hl_lines="11 19"
    /**
     * Handler for requests to Lambda function.
     */
    public class App implements RequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    
        Logger log = LogManager.getLogger();
    
        @Logging(logEvent = true)
        public APIGatewayProxyResponseEvent handleRequest(final APIGatewayProxyRequestEvent input, final Context context) {
            ...
            LoggingUtils.appendKey("test", "willBeLogged");
            ...
    
            ...
             Map<String, String> customKeys = new HashMap<>();
             customKeys.put("test", "value");
             customKeys.put("test1", "value1");
    
             LoggingUtils.appendKeys(customKeys);
            ...
        }
    }
    ```


### Removing additional keys

You can remove any additional key from entry using `LoggingUtils.removeKeys()`.

=== "App.java"

    ```java hl_lines="19 20"
    /**
     * Handler for requests to Lambda function.
     */
    public class App implements RequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    
        Logger log = LogManager.getLogger();
    
        @Logging(logEvent = true)
        public APIGatewayProxyResponseEvent handleRequest(final APIGatewayProxyRequestEvent input, final Context context) {
            ...
            LoggingUtils.appendKey("test", "willBeLogged");
            ...
            Map<String, String> customKeys = new HashMap<>();
            customKeys.put("test1", "value");
            customKeys.put("test2", "value1");
    
            LoggingUtils.appendKeys(customKeys);
            ...
            LoggingUtils.removeKey("test");
            LoggingUtils.removeKeys("test1", "test2");
            ...
        }
    }
    ```

### Clearing all state

Logger is commonly initialized in the global scope. Due to [Lambda Execution Context reuse](https://docs.aws.amazon.com/lambda/latest/dg/runtimes-context.html), 
this means that custom keys can be persisted across invocations. If you want all custom keys to be deleted, you can use 
`clearState=true` attribute on `@Logging` annotation.


=== "App.java"

    ```java hl_lines="8 12"
    /**
     * Handler for requests to Lambda function.
     */
    public class App implements RequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    
        Logger log = LogManager.getLogger();
    
        @Logging(clearState = true)
        public APIGatewayProxyResponseEvent handleRequest(final APIGatewayProxyRequestEvent input, final Context context) {
            ...
            if(input.getHeaders().get("someSpecialHeader")) {
                LoggingUtils.appendKey("specialKey", "value");
            }
            
            log.info("Collecting payment");
            ...
        }
    }
    ```
=== "#1 Request"

    ```json hl_lines="11"
	{
		"level": "INFO",
	  	"message": "Collecting payment",
		"timestamp": "2021-05-03 11:47:12,494+0200",
	  	"service": "payment",
	  	"coldStart": true,
	  	"functionName": "test",
	  	"functionMemorySize": 128,
	  	"functionArn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
	  	"lambda_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72",
        "specialKey": "value"
	}
    ```

=== "#2 Request"

    ```json
	{
		"level": "INFO",
	  	"message": "Collecting payment",
		"timestamp": "2021-05-03 11:47:12,494+0200",
	  	"service": "payment",
	  	"coldStart": true,
	  	"functionName": "test",
	  	"functionMemorySize": 128,
	  	"functionArn": "arn:aws:lambda:eu-west-1:12345678910:function:test",
	  	"lambda_request_id": "52fdfc07-2182-154f-163f-5f0f9a621d72"
	}
    ```

## Override default object mapper

You can optionally choose to override default object mapper which is used to serialize lambda function events. You might
want to supply custom object mapper in order to control how serialisation is done, for example, when you want to log only
specific fields from received event due to security.

=== "App.java"

    ```java hl_lines="9 10"
    /**
     * Handler for requests to Lambda function.
     */
    public class App implements RequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    
        Logger log = LogManager.getLogger();

        static {
            ObjectMapper objectMapper = new ObjectMapper();
            LoggingUtils.defaultObjectMapper(objectMapper);
        }
    
        @Logging(logEvent = true)
        public APIGatewayProxyResponseEvent handleRequest(final APIGatewayProxyRequestEvent input, final Context context) {
            ...
        }
    }
    ```

## Sampling debug logs

You can dynamically set a percentage of your logs to **DEBUG** level via env var `POWERTOOLS_LOGGER_SAMPLE_RATE` or
via `samplingRate` attribute on annotation. 

!!! info
    Configuration on environment variable is given precedence over sampling rate configuration on annotation, provided it's in valid value range.

=== "Sampling via annotation attribute"

    ```java hl_lines="8"
    /**
     * Handler for requests to Lambda function.
     */
    public class App implements RequestHandler<APIGatewayProxyRequestEvent, APIGatewayProxyResponseEvent> {
    
        Logger log = LogManager.getLogger();
    
        @Logging(samplingRate = 0.5)
        public APIGatewayProxyResponseEvent handleRequest(final APIGatewayProxyRequestEvent input, final Context context) {
         ...
        }
    }
    ```

=== "Sampling via environment variable"

    ```yaml hl_lines="9"
    Resources:
        HelloWorldFunction:
            Type: AWS::Serverless::Function
            Properties:
            ...
            Runtime: java8
            Environment:
                Variables:
                    POWERTOOLS_LOGGER_SAMPLE_RATE: 0.5
    ```
