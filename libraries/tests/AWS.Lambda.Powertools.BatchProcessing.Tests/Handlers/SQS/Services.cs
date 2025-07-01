using System;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS.Custom;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.SQS;

internal class Services
{
    private static readonly Lazy<IServiceProvider> LazyInstance = new(Build);

    private static ServiceCollection _services;
    public static IServiceProvider Provider => LazyInstance.Value;

    public static IServiceProvider Init()
    {
        return LazyInstance.Value;
    }

    private static IServiceProvider Build()
    {
        _services = new ServiceCollection();
        _services.AddScoped<ISqsBatchProcessor, CustomSqsBatchProcessor>();
        _services.AddScoped<ISqsRecordHandler, CustomSqsRecordHandler>();
        return _services.BuildServiceProvider();
    }
}