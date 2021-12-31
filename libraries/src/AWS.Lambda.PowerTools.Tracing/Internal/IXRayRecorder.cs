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

namespace Amazon.Lambda.PowerTools.Tracing.Internal
{
    public interface IXRayRecorder
    {
        ISegmentEmitter Emitter { get; }
        IStreamingStrategy StreamingStrategy { get; }
        
        void BeginSubsegment(string name);
        void SetNamespace(string value);
        void AddAnnotation(string key, object value);
        void AddMetadata(string nameSpace, string key, object value);
        void EndSubsegment();
        Entity GetEntity();
        void SetEntity(Entity entity);
        void AddException(Exception exception);
        void AddHttpInformation(string key, object value);
    }
}