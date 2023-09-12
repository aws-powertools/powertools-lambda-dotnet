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
        _services.AddScoped<CustomSqsBatchProcessor>();
        _services.AddScoped<CustomSqsRecordHandler>();
        return _services.BuildServiceProvider();
    }
}