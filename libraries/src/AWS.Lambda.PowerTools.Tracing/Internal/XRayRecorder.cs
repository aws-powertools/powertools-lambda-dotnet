using System;
using Amazon.XRay.Recorder.Core;

namespace Amazon.Lambda.PowerTools.Tracing.Internal
{
    internal class XRayRecorder : IXRayRecorder
    {
        private static IXRayRecorder _instance;
        public static IXRayRecorder Instance => _instance ??= new XRayRecorder();
        
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
    }
}