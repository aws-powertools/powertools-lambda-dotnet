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
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///      Creates custom metrics asynchronously by logging metrics to
///      standard output following Amazon CloudWatch Embedded Metric Format (EMF).                    <br/>
///                                                                                                   <br/>
///     Key features                                                                                  <br/>
///     ---------------------                                                                         <br/> 
///     <list type="bullet">
///         <item>
///             <description>Aggregate up to 100 metrics using a single CloudWatch EMF object (large JSON blob)</description>
///         </item>
///         <item>
///             <description>Validate against common metric definitions mistakes (metric unit, values, max dimensions, max metrics, etc)</description>
///         </item>
///         <item>
///             <description>Metrics are created asynchronously by CloudWatch service, no custom stacks needed</description>
///         </item>
///         <item>
///             <description>Context manager to create a one off metric with a different dimension</description>
///         </item>
///     </list>
///                                                                                                   <br/> 
///     Environment variables                                                                         <br/>
///     ---------------------                                                                         <br/> 
///     <list type="table">
///         <listheader>
///           <term>Variable name</term>
///           <description>Description</description>
///         </listheader>
///         <item>
///             <term>POWERTOOLS_SERVICE_NAME</term>
///             <description>string, service name</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_METRICS_NAMESPACE</term>
///             <description>string, metric namespace</description>
///         </item>
///     </list>
///                                                                                                   <br/>
///     Parameters                                                                                    <br/>
///     -----------                                                                                   <br/>
///     <list type="table">
///         <listheader>
///           <term>Parameter name</term>
///           <description>Description</description>
///         </listheader>
///         <item>
///             <term>Service</term>
///             <description>string, service name is used for metric dimension</description>
///         </item>
///         <item>
///             <term>Namespace</term>
///             <description>string, logical container where all metrics will be placed</description>
///         </item>
///         <item>
///             <term>CaptureColdStart</term>
///             <description>bool, captures cold start during Lambda execution, by default false</description>
///         </item>
///         <item>
///             <term>RaiseOnEmptyMetrics</term>
///             <description>bool, instructs metrics validation to throw exception if no metrics are provided, by default false</description>
///         </item>
///     </list>
/// </summary>
/// <example>
///     <code>
///         [Metrics(
///             Service = "Example",
///             Namespace = "ExampleNamespace",
///             CaptureColdStart = true,
///             RaiseOnEmptyMetrics = true)
///         ]
///         public async Task&lt;APIGatewayProxyResponse&gt; FunctionHandler
///              (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
///         {
///             ...
///         }
///     </code>
/// </example>
[Injection(typeof(MetricsAspect))]
public class MetricsAttribute : Attribute
{
    // /// <summary>
    // ///     The metrics instance
    // /// </summary>
    // private IMetrics _metricsInstance;

    /// <summary>
    ///     Set namespace.
    ///     The default is the environment variable <c>POWERTOOLS_METRICS_NAMESPACE</c>.
    /// </summary>
    /// <value>The namespace.</value>
    public string Namespace { get; set; }

    /// <summary>
    ///     Service name is used for metric dimension across all metrics.
    ///     This can be also set using the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
    /// </summary>
    /// <value>The service.</value>
    public string Service { get; set; }

    /// <summary>
    ///     Captures cold start during Lambda execution
    /// </summary>
    /// <value><c>true</c> if [capture cold start]; otherwise, <c>false</c>.</value>
    public bool CaptureColdStart { get; set; }

    /// <summary>
    ///     Instructs metrics validation to throw exception if no metrics are provided.
    /// </summary>
    /// <value><c>true</c> if [raise on empty metrics]; otherwise, <c>false</c>.</value>
    public bool RaiseOnEmptyMetrics { get; set; }
}
