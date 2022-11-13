using System;
using HelloWorld.Sqs;
using Microsoft.Extensions.DependencyInjection;

namespace HelloWorld;

internal class Services
{
    private static readonly Lazy<IServiceProvider> LazyInstance = new(Build);

    public static IServiceProvider Provider => LazyInstance.Value;

    public static IServiceProvider Init()
    {
        return LazyInstance.Value;
    }

    private static IServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CustomSqsBatchProcessor>();
        services.AddSingleton<CustomSqsRecordHandler>();
        return services.BuildServiceProvider();
    }
}
