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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.Lambda.Core;
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     MetricsAspect class is responsible for capturing ColdStart metric and flushing metrics on function exit.
///     Scope.Global - means aspect will operate as singleton.
/// </summary>
[Aspect(Scope.Global)]
public class MetricsAspect
{
    /// <summary>
    ///     The is cold start
    /// </summary>
    private static bool _isColdStart;

    /// <summary>
    ///     Gets the metrics instance.
    /// </summary>
    /// <value>The metrics instance.</value>
    private static IMetrics _metricsInstance;

    static MetricsAspect()
    {
        _isColdStart = true;
    }

    /// <summary>
    /// Runs before the execution of the method marked with the Metrics Attribute
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <param name="hostType"></param>
    /// <param name="method"></param>
    /// <param name="returnType"></param>
    /// <param name="triggers"></param>
    [Advice(Kind.Before)]
    public void Before(
        [Argument(Source.Instance)] object instance,
        [Argument(Source.Name)] string name,
        [Argument(Source.Arguments)] object[] args,
        [Argument(Source.Type)] Type hostType,
        [Argument(Source.Metadata)] MethodBase method,
        [Argument(Source.ReturnType)] Type returnType,
        [Argument(Source.Triggers)] Attribute[] triggers)
    {
        // Before running Function

        var trigger = triggers.OfType<MetricsAttribute>().First();

        _metricsInstance ??= Metrics.Configure(options =>
        {
            options.Namespace = trigger.Namespace;
            options.Service = trigger.Service;
            options.RaiseOnEmptyMetrics = trigger.IsRaiseOnEmptyMetricsSet ? trigger.RaiseOnEmptyMetrics : null;
            options.CaptureColdStart = trigger.IsCaptureColdStartSet ? trigger.CaptureColdStart : null;
            options.FunctionName = trigger.FunctionName;
        });

        var eventArgs = new AspectEventArgs
        {
            Instance = instance,
            Type = hostType,
            Method = method,
            Name = name,
            Args = args,
            ReturnType = returnType,
            Triggers = triggers
        };

        if (_metricsInstance.Options.CaptureColdStart != null && _metricsInstance.Options.CaptureColdStart.Value &&
            _isColdStart)
        {
            _isColdStart = false;

            var functionName = _metricsInstance.Options?.FunctionName;
            var defaultDimensions = _metricsInstance.Options?.DefaultDimensions;

            if (string.IsNullOrWhiteSpace(functionName))
            {
                functionName = GetContext(eventArgs)?.FunctionName ?? "";
            }

            if (!string.IsNullOrWhiteSpace(functionName))
            {
                defaultDimensions?.Add("FunctionName", functionName);
            }

            _metricsInstance.PushSingleMetric(
                "ColdStart",
                1.0,
                MetricUnit.Count,
                _metricsInstance.Options?.Namespace ?? "",
                _metricsInstance.Options?.Service ?? "",
                defaultDimensions
            );
        }
    }

    /// <summary>
    /// OnExit runs after the execution of the method marked with the Metrics Attribute
    /// </summary>
    [Advice(Kind.After)]
    public void Exit()
    {
        _metricsInstance.Flush();
    }


    /// <summary>
    /// Reset the aspect for testing purposes.
    /// </summary>
    internal static void ResetForTest()
    {
        _metricsInstance = null;
        _isColdStart = true;
        Metrics.ResetForTest();
    }

    /// <summary>
    /// Gets the Lambda context
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private static ILambdaContext GetContext(AspectEventArgs args)
    {
        if (args == null || args.Method == null) return null;
        var index = Array.FindIndex(args.Method.GetParameters(), p => p.ParameterType == typeof(ILambdaContext));
        if (index >= 0)
        {
            return (ILambdaContext)args.Args[index];
        }

        return null;
    }
}