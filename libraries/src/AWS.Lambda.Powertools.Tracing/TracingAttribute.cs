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

using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Tracing.Internal;

namespace AWS.Lambda.Powertools.Tracing;

/// <summary>
///     Creates an opinionated thin wrapper for AWS X-Ray .NET SDK which provides functionality to reduce the overhead of performing common tracing tasks.                            <br/>
///                                                                                                   <br/>
///     Key features                                                                                  <br/>
///     ---------------------                                                                         <br/> 
///     <list type="bullet">
///         <item>
///             <description>Helper methods to improve the developer experience for creating custom AWS X-Ray subsegments</description>
///         </item>
///         <item>
///             <description>Capture cold start as annotation</description>
///         </item>
///         <item>
///             <description>Capture function responses and full exceptions as metadata</description>
///         </item>
///         <item>
///             <description>Better experience when developing with multiple threads</description>
///         </item>
///         <item>
///             <description>Auto-patch supported modules by AWS X-Ray</description>
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
///             <term>POWERTOOLS_TRACER_CAPTURE_RESPONSE</term>
///             <description>bool, disable auto-capture response as metadata (e.g. true, false)</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_TRACER_CAPTURE_ERROR</term>
///             <description>bool, disable auto-capture error as metadata (e.g. true, false)</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_TRACE_DISABLED</term>
///             <description>bool, disable auto-capture error or response as metadata (e.g. true, false)</description>
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
///             <description>string, service name that will be appended in all tracing metadata</description>
///         </item>
///         <item>
///             <term>SegmentName</term>
///             <description>string, custom segment name for the operation, by default '## {MethodName}'</description>
///         </item>
///         <item>
///             <term>Namespace</term>
///             <description>string, namespace to current subsegment</description>
///         </item>
///         <item>
///             <term>CaptureMode</term>
///             <description>enum, capture mode to record method responses and errors (e.g. EnvironmentVariable, Response, and Error), by default EnvironmentVariable</description>
///         </item>
///     </list>
/// </summary>
/// <example>
///     <code>
///         [Tracing(
///             SegmentName = "ExampleSegment",
///             Namespace = "ExampleNamespace",
///             CaptureMode = TracingCaptureMode.ResponseAndError)
///         ]
///         public async Task&lt;APIGatewayProxyResponse&gt; FunctionHandler
///              (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
///         {
///             ...
///         }
///     </code>
/// </example>
public class TracingAttribute : MethodAspectAttribute
{
    /// <summary>
    ///     Set custom segment name for the operation.
    ///     The default is '## {MethodName}'.
    /// </summary>
    /// <value>The name of the segment.</value>
    public string SegmentName { get; set; } = "";

    /// <summary>
    ///     Set namespace to current subsegment.
    ///     The default is the environment variable <c>POWERTOOLS_SERVICE_NAME</c>.
    /// </summary>
    /// <value>The namespace.</value>
    public string Namespace { get; set; } = "";

    /// <summary>
    ///     Set capture mode to record method responses and errors.
    ///     The defaults are the environment variables <c>POWERTOOLS_TRACER_CAPTURE_RESPONSE</c> and
    ///     <c>POWERTOOLS_TRACER_CAPTURE_ERROR</c>.
    /// </summary>
    /// <value>The capture mode.</value>
    public TracingCaptureMode CaptureMode { get; set; } = TracingCaptureMode.EnvironmentVariable;

    /// <summary>
    ///     Creates the handler.
    /// </summary>
    /// <returns>IMethodAspectHandler.</returns>
    protected override IMethodAspectHandler CreateHandler()
    {
        return new TracingAspectHandler
        (
            SegmentName,
            Namespace,
            CaptureMode,
            PowertoolsConfigurations.Instance,
            XRayRecorder.Instance
        );
    }
}