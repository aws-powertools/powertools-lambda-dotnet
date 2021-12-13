using System;
using System.Collections.Generic;

namespace AWS.Lambda.PowerTools.Metrics
{
    public interface IMetrics : IDisposable
    {   
        void AddMetric(string key, double value, MetricUnit unit);
        void AddDimension(string key, string value);
        void SetDefaultDimensions(Dictionary<string, string> defaultDimension);
        void AddMetadata(string key, dynamic value);
        void PushSingleMetric(string metricName, double value, MetricUnit unit, string nameSpace = null, string service = null, Dictionary<string, string> defaultDimensions = null);
        void SetNamespace(string nameSpace);
        string GetNamespace();
        string GetService();
        string Serialize();
        void Flush(bool metricsOverflow = false);
    }
}
    