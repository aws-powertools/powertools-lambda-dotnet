using System.Collections.Generic;

namespace Amazon.LambdaPowertools.Tracing
{
    public class Tracer : ITracer
    { 
        public void Patch(List<string> modules)
        {
            throw new System.NotImplementedException();
        }

        public void PutAnnotation(string key, string value)
        {
            throw new System.NotImplementedException();
        }

        public void PutMetadata(string key, string value)
        {
            throw new System.NotImplementedException();
        }
    }
}