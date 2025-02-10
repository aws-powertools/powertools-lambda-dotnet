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
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Tracing.Internal;

namespace AWS.Lambda.Powertools.Tracing;

/// <summary>
///     Class Tracing.
/// </summary>
public static class Tracing
{
    /// <summary>
    ///     Gets entity (segment/subsegment) from the
    ///     <see cref="P:Amazon.XRay.Recorder.Core.AWSXRayRecorderImpl.TraceContext" />.
    /// </summary>
    /// <returns>The entity (segment/subsegment)</returns>
    /// <exception cref="T:Amazon.XRay.Recorder.Core.Exceptions.EntityNotAvailableException">
    ///     Thrown when the entity is not
    ///     available to get.
    /// </exception>
    public static Entity GetEntity()
    {
        return XRayRecorder.Instance.GetEntity();
    }

    /// <summary>
    ///     Set the specified entity (segment/subsegment) into
    ///     <see cref="P:Amazon.XRay.Recorder.Core.AWSXRayRecorderImpl.TraceContext" />.
    /// </summary>
    /// <param name="entity">The entity to be set</param>
    /// <exception cref="T:Amazon.XRay.Recorder.Core.Exceptions.EntityNotAvailableException">
    ///     Thrown when the entity is not
    ///     available to set
    /// </exception>
    public static void SetEntity(Entity entity)
    {
        XRayRecorder.Instance.SetEntity(entity);
    }

    /// <summary>
    ///     Adds the specified key and value as annotation to current segment.
    ///     The type of value is restricted. Only <see cref="T:System.String" />, <see cref="T:System.Int32" />,
    ///     <see cref="T:System.Int64" />,
    ///     <see cref="T:System.Double" /> and <see cref="T:System.Boolean" /> are supported.
    /// </summary>
    /// <param name="key">The key of the annotation to add.</param>
    /// <param name="value">The value of the annotation to add.</param>
    /// <exception cref="T:Amazon.XRay.Recorder.Core.Exceptions.EntityNotAvailableException">
    ///     Entity is not available in trace
    ///     context.
    /// </exception>
    public static void AddAnnotation(string key, object value)
    {
        XRayRecorder.Instance.AddAnnotation(key, value);
    }

    /// <summary>
    ///     Adds the specified key and value to metadata with given namespace.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public static void AddMetadata(string key, object value)
    {
        XRayRecorder.Instance.AddMetadata(GetNamespaceOrDefault(null), key, value);
    }

    /// <summary>
    ///     Adds the specified key and value to metadata with given namespace.
    /// </summary>
    /// <param name="nameSpace">The namespace.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public static void AddMetadata(string nameSpace, string key, object value)
    {
        XRayRecorder.Instance.AddMetadata(GetNamespaceOrDefault(nameSpace), key, value);
    }

    /// <summary>
    ///     Add the exception to current segment and also mark current segment as fault.
    /// </summary>
    /// <param name="exception">The exception to be added.</param>
    /// <exception cref="T:Amazon.XRay.Recorder.Core.Exceptions.EntityNotAvailableException">
    ///     Entity is not available in trace
    ///     context.
    /// </exception>
    public static void AddException(Exception exception)
    {
        XRayRecorder.Instance.AddException(exception);
    }

    /// <summary>
    ///     Adds the specified key and value as http information to current segment.
    /// </summary>
    /// <param name="key">The key of the http information to add.</param>
    /// <param name="value">The value of the http information to add.</param>
    /// <exception cref="T:System.ArgumentException">Key is null or empty.</exception>
    /// <exception cref="T:System.ArgumentNullException">Value is null.</exception>
    /// <exception cref="T:Amazon.XRay.Recorder.Core.Exceptions.EntityNotAvailableException">
    ///     Entity is not available in trace
    ///     context.
    /// </exception>
    public static void AddHttpInformation(string key, object value)
    {
        XRayRecorder.Instance.AddHttpInformation(key, value);
    }

    /// <summary>
    ///     Adds a new subsegment around the passed consumer. This also provides access to
    ///     the newly created subsegment.
    ///     The namespace used follows the flow as described in
    ///     <see cref="T:AWS.Lambda.Powertools.Tracing.TracingAttribute" />
    /// </summary>
    /// <param name="name">The name of the subsegment.</param>
    /// <param name="subsegment">The AWS X-Ray subsegment for the wrapped consumer.</param>
    /// <exception cref="ArgumentNullException">Thrown when the name is not provided.</exception>
    public static void WithSubsegment(string name, Action<Subsegment> subsegment)
    {
        WithSubsegment(null, name, subsegment);
    }

    /// <summary>
    ///     Adds a new subsegment around the passed consumer. This also provides access to
    ///     the newly created subsegment.
    ///     The namespace used follows the flow as described in
    ///     <see cref="T:AWS.Lambda.Powertools.Tracing.TracingAttribute" />
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <param name="name">The name of the subsegment.</param>
    /// <param name="subsegment">The AWS X-Ray subsegment for the wrapped consumer.</param>
    /// <exception cref="System.ArgumentNullException">name</exception>
    public static void WithSubsegment(string nameSpace, string name, Action<Subsegment> subsegment)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        XRayRecorder.Instance.BeginSubsegment("## " + name);
        XRayRecorder.Instance.SetNamespace(GetNamespaceOrDefault(nameSpace));
        try
        {
            subsegment?.Invoke((Subsegment) XRayRecorder.Instance.GetEntity());
        }
        finally
        {
            XRayRecorder.Instance.EndSubsegment();
        }
    }


    /// <summary>
    ///     Adds a new subsegment around the passed consumer. This also provides access to
    ///     the newly created subsegment.
    ///     This method is intended for use with multi-threaded programming where the
    ///     context is lost between threads.
    /// </summary>
    /// <param name="name">The name of the subsegment.</param>
    /// <param name="entity">The current AWS X-Ray context.</param>
    /// <param name="subsegment">The AWS X-Ray subsegment for the wrapped consumer.</param>
    /// <exception cref="ArgumentNullException">Thrown when the name is not provided.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the entity is not provided.</exception>
    public static void WithSubsegment(string name, Entity entity, Action<Subsegment> subsegment)
    {
        WithSubsegment(null, name, subsegment);
    }

    /// <summary>
    ///     Adds a new subsegment around the passed consumer. This also provides access to
    ///     the newly created subsegment.
    ///     This method is intended for use with multi-threaded programming where the
    ///     context is lost between threads.
    /// </summary>
    /// <param name="nameSpace">The namespace of the subsegment.</param>
    /// <param name="name">The name of the subsegment.</param>
    /// <param name="entity">The current AWS X-Ray context.</param>
    /// <param name="subsegment">The AWS X-Ray subsegment for the wrapped consumer.</param>
    /// <exception cref="System.ArgumentNullException">name</exception>
    /// <exception cref="System.ArgumentNullException">entity</exception>
    public static void WithSubsegment(string nameSpace, string name, Entity entity, Action<Subsegment> subsegment)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var childSubsegment = new Subsegment($"## {name}");
        entity.AddSubsegment(childSubsegment);
        childSubsegment.Sampled = entity.Sampled;
        childSubsegment.SetStartTimeToNow();
        childSubsegment.Namespace = GetNamespaceOrDefault(nameSpace);
        try
        {
            subsegment?.Invoke(childSubsegment);
        }
        finally
        {
            childSubsegment.IsInProgress = false;
            childSubsegment.Release();
            childSubsegment.SetEndTimeToNow();
            if (PowertoolsConfigurations.Instance.IsLambdaEnvironment)
            {
                if (childSubsegment.IsEmittable())
                    XRayRecorder.Instance.Emitter.Send(childSubsegment.RootSegment);
                else if (XRayRecorder.Instance.StreamingStrategy.ShouldStream(childSubsegment))
                    XRayRecorder.Instance.StreamingStrategy.Stream(childSubsegment.RootSegment,
                        XRayRecorder.Instance.Emitter);
            }
        }
    }

    /// <summary>
    ///     Gets the namespace or default.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    /// <returns>System.String.</returns>
    private static string GetNamespaceOrDefault(string nameSpace)
    {
        if (!string.IsNullOrWhiteSpace(nameSpace))
            return nameSpace;

        nameSpace = (GetEntity() as Subsegment)?.Namespace;
        if (!string.IsNullOrWhiteSpace(nameSpace))
            return nameSpace;

        return PowertoolsConfigurations.Instance.Service;
    }
    
    /// <summary>
    ///     Registers X-Ray for all instances of <see cref="Amazon.Runtime.AmazonServiceClient"/>.
    /// </summary>
    public static void RegisterForAllServices()
    {
        AWSSDKHandler.RegisterXRayForAllServices();
    }

    /// <summary>
    ///     Registers X-Ray for the given type of <see cref="Amazon.Runtime.AmazonServiceClient"/>.
    /// </summary>
    public static void Register<T>()
    {
        AWSSDKHandler.RegisterXRay<T>();
    }
}