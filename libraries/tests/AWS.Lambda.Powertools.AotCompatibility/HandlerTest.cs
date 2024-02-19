using Amazon.Lambda.TestUtilities;

namespace AWS.Lambda.Powertools.AotCompatibility;

public class HandlerTest
{   
    [Fact]
    public async Task Test1()
    {
        var handler = new Handlers.Handler();


        var context = new TestLambdaContext()
        {
            FunctionName = "PowertoolsLoggingSample-HelloWorldFunction-Gg8rhPwO7Wa1",
            FunctionVersion = "1",
            MemoryLimitInMB = 215,
            AwsRequestId = Guid.NewGuid().ToString("D")
        };

        // Act
        var response = await handler.Handle("whatever", context);

        Assert.Equal("whatever", response);
    }
}