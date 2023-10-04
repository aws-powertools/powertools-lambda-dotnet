using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

public class ExceptionFunctionHandler
{
    [Metrics(Namespace = "ns", Service = "svc")]
    public Task<string> Handle(string input)
    {
        ThisThrows();
        return Task.FromResult(input.ToUpper(CultureInfo.InvariantCulture));
    }
    
    [Metrics(Namespace = "ns", Service = "svc")]
    public Task<string> HandleDecoratorOutsideHandler(string input)
    {
        MethodDecorated();
        
        Metrics.AddMetric($"Metric Name", 1, MetricUnit.Count);

        return Task.FromResult(input.ToUpper(CultureInfo.InvariantCulture));
    }
    
    [Metrics(Namespace = "ns", Service = "svc")]
    public Task<string> HandleDecoratorOutsideHandlerException(string input)
    {
        MethodDecorated();
        
        Metrics.AddMetric($"Metric Name", 1, MetricUnit.Count);
        
        ThisThrowsDecorated();
        
        return Task.FromResult(input.ToUpper(CultureInfo.InvariantCulture));
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    private void MethodDecorated()
    {
        Metrics.AddMetric($"Metric Name", 1, MetricUnit.Count);
        Metrics.AddMetric($"Metric Name Decorated", 1, MetricUnit.Count);
    }

    private void ThisThrows()
    {
        throw new NullReferenceException();
    }
    
    [Metrics(Namespace = "ns", Service = "svc")]
    private void ThisThrowsDecorated()
    {
        throw new NullReferenceException();
    }
}