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
        
        public static void AddAnnotation(string key, object value)
        {
            XRayRecorder.Instance.AddAnnotation(key, value);
        }

        public static void AddMetadata(string key, object value)
        {
            AddMetadata(null, key, value);
        }

        public static void AddMetadata(string nameSpace, string key, object value)
        {
            XRayRecorder.Instance.AddMetadata(nameSpace, key, value);
        }
        
        public static ISegmentScope BeginSubsegmentScope(string name, Entity entity = null)
        {
            return BeginSubsegmentScope(null, name, entity);
        }

        public static ISegmentScope BeginSubsegmentScope(string nameSpace, string name, Entity entity = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            
            if (!name.StartsWith("#"))
                name = $"## {name}";
           
            return new SegmentScope(
                PowerToolsConfigurations.Instance,
                XRayRecorder.Instance,
                nameSpace,
                name,
                entity
            );
        }
    }
}