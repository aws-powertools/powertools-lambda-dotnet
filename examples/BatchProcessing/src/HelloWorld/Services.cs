/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

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
