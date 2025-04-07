using System;
using System.Collections.Generic;
using AWS.Lambda.Powertools.Common.Core;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Tests.Handlers;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.Logging.Tests.Formatter
{
    [Collection("Sequential")]
    public class LogFormattingTests
    {
        private readonly ITestOutputHelper _output;

        public LogFormattingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestNumericFormatting()
        {
            // Set culture for thread and format provider
            var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");


            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "format-test-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            // Test numeric format specifiers
            logger.LogInformation("Price: {price:0.00}", 123.4567);
            logger.LogInformation("Percentage: {percent:0.0%}", 0.1234);
            // Use explicit dollar sign instead of culture-dependent 'C'
            // The logger explicitly uses InvariantCulture when formatting values, which uses "¤" as the currency symbol. 
            // This is by design to ensure consistent logging output regardless of server culture settings.
            // By using $ directly in the format string as shown above, you bypass the culture-specific currency symbol and get the expected output in your tests.
            logger.LogInformation("Currency: {amount:$#,##0.00}", 42.5);

            logger.LogInformation("Hex: {hex:X}", 255);

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // These should all be properly formatted in the log
            Assert.Contains("\"price\":123.46", logOutput);
            Assert.Contains("\"percent\":\"12.3%\"", logOutput);
            Assert.Contains("\"amount\":\"$42.50\"", logOutput);
            Assert.Contains("\"hex\":\"FF\"", logOutput);
        }

        [Fact]
        public void TestCustomObjectFormatting()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "object-format-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.CamelCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Age = 42
            };

            // Regular object formatting (uses ToString())
            logger.LogInformation("User data: {user}", user);

            // Object serialization with @ prefix
            logger.LogInformation("User object: {@user}", user);

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // First log should use ToString()
            Assert.Contains("\"message\":\"User data: Doe, John (42)\"", logOutput);
            Assert.Contains("\"user\":\"Doe, John (42)\"", logOutput);

            // Second log should serialize the object
            Assert.Contains("\"user\":{", logOutput);
            Assert.Contains("\"firstName\":\"John\"", logOutput);
            Assert.Contains("\"lastName\":\"Doe\"", logOutput);
            Assert.Contains("\"age\":42", logOutput);
        }

        [Fact]
        public void TestComplexObjectWithIgnoredProperties()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "complex-object-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            var example = new ExampleClass
            {
                Name = "test",
                Price = 1.999,
                ThisIsBig = "big",
                ThisIsHidden = "hidden"
            };

            // Test with @ prefix for serialization
            logger.LogInformation("Example serialized: {@example}", example);

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // Should serialize the object properties
            Assert.Contains("\"example\":{", logOutput);
            Assert.Contains("\"name\":\"test\"", logOutput);
            Assert.Contains("\"price\":1.999", logOutput);
            Assert.Contains("\"this_is_big\":\"big\"", logOutput);

            // The JsonIgnore property should be excluded
            Assert.DoesNotContain("this_is_hidden", logOutput);
        }

        [Fact]
        public void TestMixedFormatting()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "mixed-format-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.PascalCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            var user = new User
            {
                FirstName = "Jane",
                LastName = "Smith",
                Age = 35
            };

            // Mix regular values with formatted values and objects
            logger.LogInformation(
                "Details: User={@user}, Price={price:$#,##0.00}, Date={date:yyyy-MM-dd}",
                user,
                123.45,
                new DateTime(2023, 4, 5)
            );

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // Verify all formatted parts
            Assert.Contains("\"User\":{", logOutput);
            Assert.Contains("\"FirstName\":\"Jane\"", logOutput);
            Assert.Contains("\"Price\":\"$123.45\"", logOutput);
            Assert.Contains("\"Date\":\"2023-04-05\"", logOutput);
        }

        [Fact]
        public void TestNestedObjectSerialization()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "nested-object-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            var parent = new ParentClass
            {
                Name = "Parent",
                Child = new ChildClass { Name = "Child" }
            };

            // Regular object formatting (uses ToString())
            logger.LogInformation("Parent: {parent}", parent);

            // Object serialization with @ prefix
            logger.LogInformation("Parent with child: {@parent}", parent);

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // Regular formatting should use ToString()
            Assert.Contains("\"parent\":\"Parent with Child\"", logOutput);

            // Serialized object should include nested structure
            Assert.Contains("\"parent\":{", logOutput);
            Assert.Contains("\"name\":\"Parent\"", logOutput);
            Assert.Contains("\"child\":{", logOutput);
            Assert.Contains("\"name\":\"Child\"", logOutput);
        }

        [Fact]
        public void TestCollectionFormatting()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "collection-format-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.CamelCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            var items = new[] { 1, 2, 3 };
            var dict = new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 42 };

            // Regular array formatting
            logger.LogInformation("Array: {items}", items);

            // Serialized array with @ prefix
            logger.LogInformation("Array serialized: {@items}", items);

            // Dictionary formatting
            logger.LogInformation("Dictionary: {dict}", dict);

            // Serialized dictionary
            logger.LogInformation("Dictionary serialized: {@dict}", dict);

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // Regular array formatting uses ToString()
            Assert.Contains("\"items\":\"System.Int32[]\"", logOutput);

            // Serialized array should include all items
            Assert.Contains("\"items\":[1,2,3]", logOutput);

            // Dictionary formatting depends on ToString() implementation
            Assert.Contains("\"dict\":\"System.Collections.Generic.Dictionary", logOutput);

            // Serialized dictionary should include all key-value pairs
            Assert.Contains("\"dict\":{", logOutput);
            Assert.Contains("\"key1\":\"value1\"", logOutput);
            Assert.Contains("\"key2\":42", logOutput);
        }

        [Fact]
        public void TestNullAndEdgeCases()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "null-edge-case-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            User user = null;

            // Test null formatting
            logger.LogInformation("Null object: {user}", user);
            logger.LogInformation("Null serialized: {@user}", user);

            // Extreme values
            logger.LogInformation("Max value: {max}", int.MaxValue);
            logger.LogInformation("Min value: {min}", int.MinValue);
            logger.LogInformation("Max double: {maxDouble}", double.MaxValue);

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // Null objects should be null in output
            Assert.Contains("\"user\":null", logOutput);

            // Extreme values should be preserved
            Assert.Contains("\"max\":2147483647", logOutput);
            Assert.Contains("\"min\":-2147483648", logOutput);
            Assert.Contains("\"max_double\":1.7976931348623157E+308", logOutput);
        }

        [Fact]
        public void TestDateTimeFormats()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "datetime-format-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.CamelCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            var date = new DateTime(2023, 12, 31, 23, 59, 59);

            // Test different date formats
            logger.LogInformation("ISO: {date:o}", date);
            logger.LogInformation("Short date: {date:d}", date);
            logger.LogInformation("Custom: {date:yyyy-MM-dd'T'HH:mm:ss.fff}", date);
            logger.LogInformation("Time only: {date:HH:mm:ss}", date);

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // Verify different formats
            Assert.Contains("\"date\":\"2023-12-31T23:59:59", logOutput); // ISO format
            Assert.Contains("\"date\":\"12/31/2023\"", logOutput); // Short date
            Assert.Contains("\"date\":\"2023-12-31T23:59:59.000\"", logOutput); // Custom
            Assert.Contains("\"date\":\"23:59:59\"", logOutput); // Time only
        }

        [Fact]
        public void TestExceptionLogging()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "exception-test-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            try
            {
                throw new InvalidOperationException("Test exception");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred with {data}", "test value");

                // Test with nested exceptions
                var outerEx = new Exception("Outer exception", ex);
                logger.LogError(outerEx, "Nested exception test");
            }

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // Verify exception details are included
            Assert.Contains("\"message\":\"An error occurred with test value\"", logOutput);
            Assert.Contains("\"exception\":{", logOutput);
            Assert.Contains("\"type\":\"System.InvalidOperationException\"", logOutput);
            Assert.Contains("\"message\":\"Test exception\"", logOutput);
            Assert.Contains("\"stack_trace\":", logOutput);

            // Verify nested exception details
            Assert.Contains("\"message\":\"Nested exception test\"", logOutput);
            Assert.Contains("\"inner_exception\":{", logOutput);
        }

        [Fact]
        public void TestScopedLogging()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "scope-test-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            // Log without any scope
            logger.LogInformation("Outside any scope");

            // Create a scope and log within it
            using (logger.BeginScope(new { RequestId = "req-123", UserId = "user-456" }))
            {
                logger.LogInformation("Inside first scope");

                // Nested scope
                using (logger.BeginScope(new { OperationId = "op-789" }))
                {
                    logger.LogInformation("Inside nested scope");
                }

                logger.LogInformation("Back to first scope");
            }

            // Back outside all scopes
            logger.LogInformation("Outside all scopes again");

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // Verify scope information is included correctly
            Assert.Contains("\"message\":\"Inside first scope\"", logOutput);
            Assert.Contains("\"request_id\":\"req-123\"", logOutput);
            Assert.Contains("\"user_id\":\"user-456\"", logOutput);

            // Nested scope should include both scopes' data
            Assert.Contains("\"message\":\"Inside nested scope\"", logOutput);
            Assert.Contains("\"operation_id\":\"op-789\"", logOutput);
        }

        [Fact]
        public void TestDifferentLogLevels()
        {
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "log-level-test-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;
                    config.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            logger.LogTrace("This is a trace message");
            logger.LogDebug("This is a debug message");
            logger.LogInformation("This is an info message");
            logger.LogWarning("This is a warning message");
            logger.LogError("This is an error message");
            logger.LogCritical("This is a critical message");

            var logOutput = output.ToString();
            _output.WriteLine(logOutput);

            // Trace shouldn't be logged (below default)
            Assert.DoesNotContain("\"level\":\"Trace\"", logOutput);

            // Debug and above should be logged
            Assert.Contains("\"level\":\"Debug\"", logOutput);
            Assert.Contains("\"level\":\"Information\"", logOutput);
            Assert.Contains("\"level\":\"Warning\"", logOutput);
            Assert.Contains("\"level\":\"Error\"", logOutput);
            Assert.Contains("\"level\":\"Critical\"", logOutput);
        }
        
        [Fact]
        public void Should_Log_Multiple_Formats_No_Duplicates()
        {
            var output = new TestLoggerOutput();
            LambdaLifecycleTracker.Reset();
            LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "log-level-test-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Age = 42,
                TimeStamp = "FakeTime"
            };
            
            Logger.LogInformation<User>(user, "{Name} and is {Age} years old", new object[]{user.Name, user.Age});
            Assert.Contains("\"first_name\":\"John\"", output.ToString());
            Assert.Contains("\"last_name\":\"Doe\"", output.ToString());
            Assert.Contains("\"age\":42", output.ToString());
            Assert.Contains("\"name\":\"AWS.Lambda.Powertools.Logging.Logger\"", output.ToString()); // does not override name
            
            output.Clear();
            
            Logger.LogInformation("{level}", user);
            Assert.Contains("\"level\":\"Information\"", output.ToString()); // does not override level
            Assert.Contains("\"message\":\"Doe, John (42)\"", output.ToString()); // does not override message
            Assert.DoesNotContain("\"timestamp\":\"FakeTime\"", output.ToString());
            
            output.Clear();

            Logger.LogInformation("{coldstart}", user); // still not sure if convert to PascalCase to compare or not
            Assert.Contains("\"cold_start\":true", output.ToString());
            
            output.Clear();
            
            Logger.AppendKey("level", "Override");
            Logger.AppendKey("message", "Override");
            Logger.AppendKey("timestamp", "Override");
            Logger.AppendKey("name", "Override");
            Logger.AppendKey("service", "Override");
            Logger.AppendKey("cold_start", "Override");
            Logger.AppendKey("message2", "Its ok!");
            
            Logger.LogInformation("no override");
            Assert.DoesNotContain("\"level\":\"Override\"", output.ToString());
            Assert.DoesNotContain("\"message\":\"Override\"", output.ToString());
            Assert.DoesNotContain("\"timestamp\":\"Override\"", output.ToString());
            Assert.DoesNotContain("\"name\":\"Override\"", output.ToString());
            Assert.DoesNotContain("\"service\":\"Override\"", output.ToString());
            Assert.DoesNotContain("\"cold_start\":\"Override\"", output.ToString());
            Assert.Contains("\"message2\":\"Its ok!\"", output.ToString());
            Assert.Contains("\"level\":\"Information\"", output.ToString());
        }
        
        [Fact]
        public void Should_Log_Multiple_Formats()
        {
            LambdaLifecycleTracker.Reset();
            var output = new TestLoggerOutput();
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddPowertoolsLogger(config =>
                {
                    config.Service = "log-level-test-service";
                    config.MinimumLogLevel = LogLevel.Debug;
                    config.LoggerOutputCase = LoggerOutputCase.SnakeCase;
                    config.LogOutput = output;
                });
            }).CreatePowertoolsLogger();

            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Age = 42
            };
            
            Logger.LogInformation<User>(user, "{Name} is {Age} years old", new object[]{user.FirstName, user.Age});
            
            var logOutput = output.ToString();
            Assert.Contains("\"level\":\"Information\"", logOutput);
            Assert.Contains("\"message\":\"John is 42 years old\"", logOutput);
            Assert.Contains("\"service\":\"log-level-test-service\"", logOutput);
            Assert.Contains("\"name\":\"AWS.Lambda.Powertools.Logging.Logger\"", logOutput);
            Assert.Contains("\"first_name\":\"John\"", logOutput);
            Assert.Contains("\"last_name\":\"Doe\"", logOutput);
            Assert.Contains("\"age\":42", logOutput);
            
            output.Clear();
            
            // Message template string
            Logger.LogInformation("{user}", user);
            
            logOutput = output.ToString();
            Assert.Contains("\"level\":\"Information\"", logOutput);
            Assert.Contains("\"message\":\"Doe, John (42)\"", logOutput);
            Assert.Contains("\"service\":\"log-level-test-service\"", logOutput);
            Assert.Contains("\"name\":\"AWS.Lambda.Powertools.Logging.Logger\"", logOutput);
            Assert.Contains("\"user\":\"Doe, John (42)\"", logOutput);
            // Verify user properties are NOT included in output (since @ prefix wasn't used)
            Assert.DoesNotContain("\"first_name\":", logOutput);
            Assert.DoesNotContain("\"last_name\":", logOutput);
            Assert.DoesNotContain("\"age\":", logOutput);
            
            output.Clear();
            
            // Object serialization with @ prefix
            Logger.LogInformation("{@user}", user);
            
            logOutput = output.ToString();
            Assert.Contains("\"level\":\"Information\"", logOutput);
            Assert.Contains("\"message\":\"Doe, John (42)\"", logOutput);
            Assert.Contains("\"service\":\"log-level-test-service\"", logOutput);
            Assert.Contains("\"cold_start\":true", logOutput);
            Assert.Contains("\"name\":\"AWS.Lambda.Powertools.Logging.Logger\"", logOutput);
            // Verify serialized user object with all properties
            Assert.Contains("\"user\":{", logOutput);
            Assert.Contains("\"first_name\":\"John\"", logOutput);
            Assert.Contains("\"last_name\":\"Doe\"", logOutput);
            Assert.Contains("\"age\":42", logOutput);
            Assert.Contains("\"name\":\"John Doe\"", logOutput);
            Assert.Contains("\"time_stamp\":null", logOutput);
            Assert.Contains("}", logOutput);
            
            output.Clear();
            
            Logger.LogInformation("{cold_start}", false);
            
            logOutput = output.ToString();
            // Assert that the reserved field wasn't replaced
            Assert.Contains("\"cold_start\":true", logOutput);
            Assert.DoesNotContain("\"cold_start\":false", logOutput);

            output.Clear();
            
            Logger.AppendKey("level", "fakeLevel");
            Logger.LogInformation("no override");
            
            logOutput = output.ToString();
            
            Assert.Contains("\"level\":\"Information\"", logOutput);
            Assert.DoesNotContain("\"level\":\"fakeLevel\"", logOutput);
            
            _output.WriteLine(logOutput);  

        }

        public class ParentClass
        {
            public string Name { get; set; }
            public ChildClass Child { get; set; }

            public override string ToString()
            {
                return $"Parent with Child";
            }
        }

        public class ChildClass
        {
            public string Name { get; set; }

            public override string ToString()
            {
                return $"Child: {Name}";
            }
        }

        public class Node
        {
            public string Name { get; set; }
            public Node Parent { get; set; }
            public List<Node> Children { get; set; } = new List<Node>();

            public override string ToString()
            {
                return $"Node: {Name}";
            }
        }

        public class User
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int Age { get; set; }
            public string Name => $"{FirstName} {LastName}";
            public string TimeStamp { get; set; }

            public override string ToString()
            {
                return $"{LastName}, {FirstName} ({Age})";
            }
        }
    }
}