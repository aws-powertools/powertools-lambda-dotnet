using System;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.PowerTools.Core;

namespace Amazon.Lambda.PowerTools.Tracing.Internal
{
    internal class SegmentScope : ISegmentScope
    {
        private readonly IPowerToolsConfigurations _powerToolsConfigurations;
        private readonly IXRayRecorder _xRayRecorder;
        private readonly Subsegment _current;
        private readonly string _nameSpace;

        internal SegmentScope(
            IPowerToolsConfigurations powerToolsConfigurations, 
            IXRayRecorder xRayRecorder,
            string nameSpace,
            string name,
            Entity entity)
        {
            _powerToolsConfigurations = powerToolsConfigurations;
            _xRayRecorder = xRayRecorder;
            _nameSpace = GetNamespaceOrDefault(nameSpace);
            
            if (entity is null)
            {
                _xRayRecorder.BeginSubsegment(name);
                _xRayRecorder.SetNamespace(_nameSpace);
            }
            else
            {
                _current = new Subsegment(name);
                entity.AddSubsegment(_current);
                _current.Sampled = entity.Sampled;
                _current.SetStartTimeToNow();
                _current.Namespace = _nameSpace;
            }
        }

        private string GetNamespaceOrDefault(string nameSpace)
        {
            return string.IsNullOrWhiteSpace(nameSpace) ? _powerToolsConfigurations.ServiceName : nameSpace;
        }
        
        public Entity GetEntity()
        {
            return _current ?? _xRayRecorder.GetEntity();
        }
        
        public void AddAnnotation(string key, object value)
        {
            if(_current is null)
                _xRayRecorder.AddAnnotation(key, value);
            else 
                _current.AddAnnotation(key, value);
                
        }

        public void AddMetadata(string key, object value)
        {
            AddMetadata(_nameSpace, key, value);
        }

        public void AddMetadata(string nameSpace, string key, object value)
        {
            if (_current is null)
                _xRayRecorder.AddMetadata(GetNamespaceOrDefault(nameSpace), key, value);
            else
                _current.AddMetadata(GetNamespaceOrDefault(nameSpace), key, value);
        }
        
        public void AddException(Exception exception)
        {
            if (_current is null)
                _xRayRecorder.AddException(exception);
            else
                _current.AddException(exception);
        }

        public void Dispose()
        {
            if (_current is null)
            {
                _xRayRecorder.EndSubsegment();
                return;
            }
            
            _current.IsInProgress = false;
            _current.Release();
            _current.SetEndTimeToNow();
            if (_current.IsEmittable())
                _xRayRecorder.Emitter.Send(_current.RootSegment);
            else if (_xRayRecorder.StreamingStrategy.ShouldStream(_current))
                _xRayRecorder.StreamingStrategy.Stream(_current.RootSegment, _xRayRecorder.Emitter);
        }
    }
}