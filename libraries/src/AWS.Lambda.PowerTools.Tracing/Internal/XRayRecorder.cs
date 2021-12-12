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
        
        public void BeginSubsegment(string name, DateTime? timestamp = null)
        {
            AWSXRayRecorder.Instance.BeginSubsegment(name, timestamp);
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