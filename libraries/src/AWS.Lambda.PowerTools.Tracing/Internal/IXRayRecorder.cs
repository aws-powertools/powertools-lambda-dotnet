using System;

namespace Amazon.Lambda.PowerTools.Tracing.Internal
{
    public interface IXRayRecorder
    {
        void BeginSubsegment(string name, DateTime? timestamp = null);
        void SetNamespace(string value);
        void AddAnnotation(string key, object value);
        void AddMetadata(string nameSpace, string key, object value);
        void EndSubsegment();
    }
}