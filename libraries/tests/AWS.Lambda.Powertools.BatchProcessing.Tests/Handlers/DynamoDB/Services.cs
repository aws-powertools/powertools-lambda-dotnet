using System;
using AWS.Lambda.Powertools.BatchProcessing.DynamoDb;
using AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB.Custom;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Lambda.Powertools.BatchProcessing.Tests.Handlers.DynamoDB;

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
        _services.AddScoped<IDynamoDbStreamBatchProcessor, CustomDynamoDbStreamBatchProcessor>();
        _services.AddScoped<IDynamoDbStreamRecordHandler, CustomDynamoDbStreamRecordHandler>();
        return _services.BuildServiceProvider();
    }
}