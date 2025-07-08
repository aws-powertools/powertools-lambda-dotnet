using System;
using AWS.Lambda.Powertools.BatchProcessing.Kinesis;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis.Custom;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.Kinesis;

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
        _services.AddScoped<IKinesisEventBatchProcessor, CustomKinesisEventBatchProcessor>();
        _services.AddScoped<IKinesisEventRecordHandler, CustomKinesisEventRecordHandler>();
        return _services.BuildServiceProvider();
    }
}