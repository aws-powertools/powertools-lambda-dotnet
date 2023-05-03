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
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Strategies;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Tracing.Internal;

/// <summary>
///     Class XRayRecorder.
///     Implements the <see cref="IXRayRecorder" />
/// </summary>
/// <seealso cref="IXRayRecorder" />
internal class XRayRecorder : IXRayRecorder
{
    private static IAWSXRayRecorder _awsxRayRecorder;
    private static IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     The instance
    /// </summary>
    private static IXRayRecorder _instance;

    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static IXRayRecorder Instance => _instance ??= new XRayRecorder(AWSXRayRecorder.Instance, PowertoolsConfigurations.Instance);

    public XRayRecorder(IAWSXRayRecorder awsxRayRecorder, IPowertoolsConfigurations powertoolsConfigurations)
    {
        _instance = this;
        _powertoolsConfigurations = powertoolsConfigurations;
        _powertoolsConfigurations.SetExecutionEnvironment(this);
        _isLambda = _powertoolsConfigurations.IsLambdaEnvironment;
        _awsxRayRecorder = awsxRayRecorder;
    }

    /// <summary>
    ///     Checks whether current execution is in AWS Lambda.
    /// </summary>
    /// <returns>Returns true if current execution is in AWS Lambda.</returns>
    private static bool _isLambda; 

    /// <summary>
    ///     Gets the emitter.
    /// </summary>
    /// <value>The emitter.</value>
    public ISegmentEmitter Emitter => _isLambda ? _awsxRayRecorder.Emitter : null;

    /// <summary>
    ///     Gets the streaming strategy.
    /// </summary>
    /// <value>The streaming strategy.</value>
    public IStreamingStrategy StreamingStrategy => _isLambda ? _awsxRayRecorder.StreamingStrategy : null;

    /// <summary>
    ///     Begins the subsegment.
    /// </summary>
    /// <param name="name">The name.</param>
    public void BeginSubsegment(string name)
    {
        if (_isLambda)
            _awsxRayRecorder.BeginSubsegment(name);
    }

    /// <summary>
    ///     Sets the namespace.
    /// </summary>
    /// <param name="value">The value.</param>
    public void SetNamespace(string value)
    {
        if (_isLambda)
            _awsxRayRecorder.SetNamespace(value);
    }

    /// <summary>
    ///     Adds the annotation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void AddAnnotation(string key, object value)
    {
        if (_isLambda)
            _awsxRayRecorder.AddAnnotation(key, value);
    }

    /// <summary>
    ///     Adds the metadata.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void AddMetadata(string nameSpace, string key, object value)
    {
        if (_isLambda)
            _awsxRayRecorder.AddMetadata(nameSpace, key, value);
    }

    /// <summary>
    ///     Ends the subsegment.
    /// </summary>
    public void EndSubsegment()
    {
        if (_isLambda)
            _awsxRayRecorder.EndSubsegment();
    }

    /// <summary>
    ///     Gets the entity.
    /// </summary>
    /// <returns>Entity.</returns>
    public Entity GetEntity()
    {
        return _isLambda
            ? _awsxRayRecorder.TraceContext.GetEntity()
            : new Subsegment("Root");
    }

    /// <summary>
    ///     Sets the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    public void SetEntity(Entity entity)
    {
        if (_isLambda)
            _awsxRayRecorder.TraceContext.SetEntity(entity);
    }

    /// <summary>
    ///     Adds the exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public void AddException(Exception exception)
    {
        if (_isLambda)
            _awsxRayRecorder.AddException(exception);
    }

    /// <summary>
    ///     Adds the HTTP information.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void AddHttpInformation(string key, object value)
    {
        if (_isLambda)
            _awsxRayRecorder.AddHttpInformation(key, value);
    }
}