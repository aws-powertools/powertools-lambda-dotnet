using Amazon.Lambda.TestUtilities;

namespace AWS.Lambda.Powertools.AotCompatibility;

public class UnitTest1
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
        Task Handle() => handler.Handle("whatever", context);

        var tracedException = await Assert.ThrowsAsync<NullReferenceException>(Handle);
        Assert.StartsWith("at AWS.Lambda.Powertools.AotCompatibility.Handlers.Handler.ThisThrows()",
            tracedException.StackTrace?.TrimStart());
    }
}