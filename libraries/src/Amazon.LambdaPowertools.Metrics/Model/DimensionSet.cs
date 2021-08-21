using System.Collections.Generic;
using System.Linq;

namespace Amazon.LambdaPowertools.Metrics.Model
{
    public class DimensionSet
    {
        internal Dictionary<string, string> Dimensions { get; } = new Dictionary<string, string>(); 

        public DimensionSet(string key, string value)
        {
            Dimensions[key] = value;
        }

        public List<string> DimensionKeys => Dimensions.Keys.ToList();
    }
}