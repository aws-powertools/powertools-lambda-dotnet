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

namespace AWS.Lambda.PowerTools.Tracing.Internal;

/// <summary>
///     Class XRayRecorder.
///     Implements the <see cref="AWS.Lambda.PowerTools.Tracing.Internal.IXRayRecorder" />
/// </summary>
/// <seealso cref="AWS.Lambda.PowerTools.Tracing.Internal.IXRayRecorder" />
internal class XRayRecorder : IXRayRecorder
{
    /// <summary>
    ///     The instance
    /// </summary>
    private static IXRayRecorder _instance;

    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static IXRayRecorder Instance => _instance ??= new XRayRecorder();

    /// <summary>
    ///     Gets the emitter.
    /// </summary>
    /// <value>The emitter.</value>
    public ISegmentEmitter Emitter => AWSXRayRecorder.Instance.Emitter;

    /// <summary>
    ///     Gets the streaming strategy.
    /// </summary>
    /// <value>The streaming strategy.</value>
    public IStreamingStrategy StreamingStrategy => AWSXRayRecorder.Instance.StreamingStrategy;

    /// <summary>
    ///     Begins the subsegment.
    /// </summary>
    /// <param name="name">The name.</param>
    public void BeginSubsegment(string name)
    {
        AWSXRayRecorder.Instance.BeginSubsegment(name);
    }

    /// <summary>
    ///     Sets the namespace.
    /// </summary>
    /// <param name="value">The value.</param>
    public void SetNamespace(string value)
    {
        AWSXRayRecorder.Instance.SetNamespace(value);
    }

    /// <summary>
    ///     Adds the annotation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void AddAnnotation(string key, object value)
    {
        AWSXRayRecorder.Instance.AddAnnotation(key, value);
    }

    /// <summary>
    ///     Adds the metadata.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void AddMetadata(string nameSpace, string key, object value)
    {
        AWSXRayRecorder.Instance.AddMetadata(nameSpace, key, value);
    }

    /// <summary>
    ///     Ends the subsegment.
    /// </summary>
    public void EndSubsegment()
    {
        AWSXRayRecorder.Instance.EndSubsegment();
    }

    /// <summary>
    ///     Gets the entity.
    /// </summary>
    /// <returns>Entity.</returns>
    public Entity GetEntity()
    {
        return AWSXRayRecorder.Instance.GetEntity();
    }

    /// <summary>
    ///     Sets the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    public void SetEntity(Entity entity)
    {
        AWSXRayRecorder.Instance.SetEntity(entity);
    }

    /// <summary>
    ///     Adds the exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public void AddException(Exception exception)
    {
        AWSXRayRecorder.Instance.AddException(exception);
    }

    /// <summary>
    ///     Adds the HTTP information.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void AddHttpInformation(string key, object value)
    {
        AWSXRayRecorder.Instance.AddHttpInformation(key, value);
    }
}