using System;

namespace Amazon.LambdaPowertools
{
    public static class PowertoolsConfig
    {
        public static int MaxDimensions = 9;
        public static int MaxMetrics = 100;

        private static string _service;
        public static string Service
        {
            get
            {
                if (!string.IsNullOrEmpty(_service))
                {
                    return _service;
                }
                else if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POWERTOOLS_SERVICE_NAME")))
                {
                    return Environment.GetEnvironmentVariable("POWERTOOLS_SERVICE_NAME");
                }
                else {
                    return "service_undefined";
                }
            }

            set {
                _service = value;
            }
        }
    }
}