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