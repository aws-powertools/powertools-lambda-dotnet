using System;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.PowerTools.Core;

namespace Amazon.Lambda.PowerTools.Tracing.Internal
{
    internal class TracingHandler
    {
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        private readonly IXRayRecorder _xRayRecorder;
        
        internal TracingHandler(IPowerToolsConfigurations powerToolsConfigurations, IXRayRecorder xRayRecorder)
        {
            _powerToolsConfigurations = powerToolsConfigurations;
            _xRayRecorder = xRayRecorder;
        }
        
        public Entity GetEntity()
        {
            return _xRayRecorder.GetEntity();
        }

        public void AddAnnotation(string key, object value)
        {
            _xRayRecorder.AddAnnotation(key, value);
        }

        public void AddMetadata(string key, object value)
        {
            var nameSpace = (_xRayRecorder.GetEntity() as Subsegment)?.Namespace;
            if (string.IsNullOrWhiteSpace(nameSpace))
                nameSpace = _powerToolsConfigurations.ServiceName;
            AddMetadata(nameSpace, key, value);
        }

        public void AddMetadata(string nameSpace, string key, object value)
        {
            _xRayRecorder.AddMetadata(nameSpace, key, value);
        }

        public void WithEntitySubsegment(string name, Entity entity, Action<Subsegment> subsegment)
        {
            WithEntitySubsegment(_powerToolsConfigurations.ServiceName, name, entity, subsegment);
        }

        public void WithEntitySubsegment(string nameSpace, string name, Entity entity, Action<Subsegment> subsegment)
        {
            _xRayRecorder.SetEntity(entity);
            var segment = new Subsegment("## " + name);
            try
            {
                entity.AddSubsegment(segment);
                segment.Sampled = entity.Sampled;
                segment.SetStartTimeToNow();
                segment.Namespace = nameSpace;
                subsegment.Invoke(segment);
            }
            finally
            {
                segment.IsInProgress = false;
                segment.Release();
                segment.SetEndTimeToNow();
                if (segment.IsEmittable())
                    _xRayRecorder.Emitter.Send(segment.RootSegment);
                else if (_xRayRecorder.StreamingStrategy.ShouldStream(segment))
                    _xRayRecorder.StreamingStrategy.Stream(segment.RootSegment, _xRayRecorder.Emitter);
            }
        }

        public void WithSubsegment(string name, Action<Subsegment> subsegment)
        {
            WithSubsegment(_powerToolsConfigurations.ServiceName, name, subsegment);
        }
        
        public void WithSubsegment(string nameSpace, string name, Action<Subsegment> subsegment)
        {
            _xRayRecorder.BeginSubsegment("## " + name);
            try
            {
                var segment = (Subsegment) _xRayRecorder.GetEntity();
                segment.Namespace = nameSpace;
                subsegment.Invoke(segment);
            }
            finally
            {
                _xRayRecorder.EndSubsegment();
            }
        }
    }
}