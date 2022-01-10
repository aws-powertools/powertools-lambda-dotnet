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
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Strategies;

namespace AWS.Lambda.Powertools.Tracing.Internal;

/// <summary>
///     Interface IXRayRecorder
/// </summary>
public interface IXRayRecorder
{
    /// <summary>
    ///     Gets the emitter.
    /// </summary>
    /// <value>The emitter.</value>
    ISegmentEmitter Emitter { get; }

    /// <summary>
    ///     Gets the streaming strategy.
    /// </summary>
    /// <value>The streaming strategy.</value>
    IStreamingStrategy StreamingStrategy { get; }

    /// <summary>
    ///     Begins the subsegment.
    /// </summary>
    /// <param name="name">The name.</param>
    void BeginSubsegment(string name);

    /// <summary>
    ///     Sets the namespace.
    /// </summary>
    /// <param name="value">The value.</param>
    void SetNamespace(string value);

    /// <summary>
    ///     Adds the annotation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void AddAnnotation(string key, object value);

    /// <summary>
    ///     Adds the metadata.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void AddMetadata(string nameSpace, string key, object value);

    /// <summary>
    ///     Ends the subsegment.
    /// </summary>
    void EndSubsegment();

    /// <summary>
    ///     Gets the entity.
    /// </summary>
    /// <returns>Entity.</returns>
    Entity GetEntity();

    /// <summary>
    ///     Sets the entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    void SetEntity(Entity entity);

    /// <summary>
    ///     Adds the exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    void AddException(Exception exception);

    /// <summary>
    ///     Adds the HTTP information.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void AddHttpInformation(string key, object value);
}