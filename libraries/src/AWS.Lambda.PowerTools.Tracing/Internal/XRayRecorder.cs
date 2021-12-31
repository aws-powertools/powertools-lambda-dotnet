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

namespace Amazon.Lambda.PowerTools.Tracing.Internal
{
    internal class XRayRecorder : IXRayRecorder
    {
        private static IXRayRecorder _instance;
        public static IXRayRecorder Instance => _instance ??= new XRayRecorder();
        
        public ISegmentEmitter Emitter => AWSXRayRecorder.Instance.Emitter;
        public IStreamingStrategy StreamingStrategy => AWSXRayRecorder.Instance.StreamingStrategy;
        
        public void BeginSubsegment(string name)
        {
            AWSXRayRecorder.Instance.BeginSubsegment(name);
        }

        public void SetNamespace(string value)
        {
            AWSXRayRecorder.Instance.SetNamespace(value);
        }

        public void AddAnnotation(string key, object value)
        {
            AWSXRayRecorder.Instance.AddAnnotation(key, value);
        }

        public void AddMetadata(string nameSpace, string key, object value)
        {
            AWSXRayRecorder.Instance.AddMetadata(nameSpace, key, value);
        }
        
        public void EndSubsegment()
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }

        public Entity GetEntity()
        {
            return AWSXRayRecorder.Instance.GetEntity();
        }
        
        public void SetEntity(Entity entity)
        {
            AWSXRayRecorder.Instance.SetEntity(entity);
        }
        
        public void AddException(Exception exception)
        {
            AWSXRayRecorder.Instance.AddException(exception);
        }

        public void AddHttpInformation(string key, object value)
        {
            AWSXRayRecorder.Instance.AddHttpInformation(key, value);
        }
    }
}