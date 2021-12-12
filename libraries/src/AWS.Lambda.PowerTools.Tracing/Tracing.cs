using System;
using Amazon.Lambda.PowerTools.Tracing.Internal;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.PowerTools.Core;

namespace Amazon.Lambda.PowerTools.Tracing
{
    public static class Tracing
    {
        public static Entity GetEntity()
        {
            return XRayRecorder.Instance.GetEntity();
        }
        
        public static void SetEntity(Entity entity)
        {
            XRayRecorder.Instance.SetEntity(entity);
        }
        
        public static void AddAnnotation(string key, object value)
        {
            XRayRecorder.Instance.AddAnnotation(key, value);
        }

        public static void AddMetadata(string key, object value)
        {
            XRayRecorder.Instance.AddMetadata(GetNamespaceOrDefault(null), key, value);
        }

        public static void AddMetadata(string nameSpace, string key, object value)
        {
            XRayRecorder.Instance.AddMetadata(GetNamespaceOrDefault(nameSpace), key, value);
        }

        public static void AddException(Exception exception)
        {
            XRayRecorder.Instance.AddException(exception);
        }
        
        public static void AddHttpInformation(string key, object value)
        {
            XRayRecorder.Instance.AddHttpInformation(key, value);
        }
        
        public static void WithSubsegment(string name, Action<Subsegment> subsegment) {
            WithSubsegment(null, name, subsegment);
        }

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
        
        public static void WithSubsegment(string name, Entity entity, Action<Subsegment> subsegment) {
            WithSubsegment(null, name, subsegment);
        }

        public static void WithSubsegment(string nameSpace, string name, Entity entity, Action<Subsegment> subsegment)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            
            if(entity is null)
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
                if (childSubsegment.IsEmittable())
                    XRayRecorder.Instance.Emitter.Send(childSubsegment.RootSegment);
                else if (XRayRecorder.Instance.StreamingStrategy.ShouldStream(childSubsegment))
                    XRayRecorder.Instance.StreamingStrategy.Stream(childSubsegment.RootSegment,
                        XRayRecorder.Instance.Emitter);
            }
        }

        private static string GetNamespaceOrDefault(string nameSpace)
        {
            if (!string.IsNullOrWhiteSpace(nameSpace))
                return nameSpace;
            
            nameSpace = (GetEntity() as Subsegment)?.Namespace;
            if (!string.IsNullOrWhiteSpace(nameSpace))
                return nameSpace;
            
            return PowerToolsConfigurations.Instance.ServiceName;
        }
    }
}