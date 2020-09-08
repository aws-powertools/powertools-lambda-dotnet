using System.Collections.Generic;

namespace Amazon.LambdaPowertools.Tracing
{
    public interface ITracer
    {
        public void Patch(List<string> modules);
        public void PutAnnotation(string key, string value);
        public void PutMetadata(string key, string value) ;
    }
}