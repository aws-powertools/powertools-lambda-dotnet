using System.Text.Json;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Xunit.Abstractions;
using InvalidOperationException = Amazon.CloudWatchLogs.Model.InvalidOperationException;

namespace Function.Tests;

public abstract class TestBase
{
    protected readonly ITestOutputHelper Output;
    
    protected TestBase(ITestOutputHelper output)
    {
        Output = output;
    }
    
    protected async Task<bool> WaitForSuccessInLogs(string functionName, string messageToFind, TimeSpan timeout)
    {
        using var logsClient = new AmazonCloudWatchLogsClient();
        var logGroupName = $"/aws/lambda/{functionName}";
        
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var response = await logsClient.DescribeLogStreamsAsync(new DescribeLogStreamsRequest
                {
                    LogGroupName = logGroupName,
                    OrderBy = "LastEventTime",
                    Descending = true,
                    Limit = 5
                });

                foreach (var stream in response.LogStreams)
                {
                    var events = await logsClient.GetLogEventsAsync(new GetLogEventsRequest
                    {
                        LogGroupName = logGroupName,
                        LogStreamName = stream.LogStreamName,
                        StartTime = DateTime.UtcNow.AddMinutes(-5)
                    });
                    
                    if (events.Events.Any(e => e.Message.Contains(messageToFind)))
                    {
                        return true;
                    }
                }
                
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Output.WriteLine($"Error checking logs: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
        
        return false;
    }
    
    protected T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize");
    }
    
    protected string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}