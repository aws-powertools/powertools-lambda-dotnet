using System;
using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace Amazon.Lambda.PowerTools.Tracing
{
    public interface ISegmentScope : IDisposable
    {
        Entity GetEntity();
        void AddAnnotation(string key, object value);
        void AddMetadata(string key, object value);
        void AddMetadata(string nameSpace, string key, object value);
    }
}