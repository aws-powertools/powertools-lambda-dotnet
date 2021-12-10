using System;
using Amazon.Lambda.PowerTools.Tracing.Internal;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using AWS.Lambda.PowerTools.Core;

namespace Amazon.Lambda.PowerTools.Tracing
{
    public static class Tracing
    {
        private static TracingHandler _instance;
        private static TracingHandler Instance => _instance ??=
            new TracingHandler(PowerToolsConfigurations.Instance, XRayRecorder.Instance);

        public static Entity GetEntity()
        {
            return Instance.GetEntity();
        }
        
        public static void AddAnnotation(string key, object value)
        {
            Instance.AddAnnotation(key, value);
        }

        public static void AddMetadata(string key, object value)
        {
            Instance.AddMetadata(key, value);
        }

        public static void AddMetadata(string nameSpace, string key, object value)
        {
            Instance.AddMetadata(nameSpace, key, value);
        }

        public static void WithEntitySubsegment(string name, Entity entity, Action<Subsegment> subsegment)
        {
            Instance.WithEntitySubsegment(name, entity, subsegment);
        }

        public static void WithEntitySubsegment(string nameSpace, string name, Entity entity,
            Action<Subsegment> subsegment)
        {
            Instance.WithEntitySubsegment(nameSpace, name, entity, subsegment);
        }

        public static void WithSubsegment(string name, Action<Subsegment> subsegment)
        {
            Instance.WithSubsegment(name, subsegment);
        }

        public static void WithSubsegment(string nameSpace, string name, Action<Subsegment> subsegment)
        {
            Instance.WithSubsegment(nameSpace, name, subsegment);
        }
    }
}