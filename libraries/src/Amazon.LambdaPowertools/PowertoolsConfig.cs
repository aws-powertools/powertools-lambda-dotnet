using System;

namespace Amazon.LambdaPowertools
{
    public static class PowertoolsSettings
    {
        private static string _namespace;
        public static string Namespace
        {
            get
            {
                if (!String.IsNullOrEmpty(_namespace))
                {
                    return _namespace;
                }
                else
                {
                    return Environment.GetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE");
                }
            }
        }

        private static string _service;
        public static string Service
        {
            get
            {
                if (!String.IsNullOrEmpty(_service))
                {
                    return _service;
                }
                else
                {
                    return Environment.GetEnvironmentVariable("POWERTOOLS_SERVICE_NAME");
                }
            }
        }

        public static void SetNamespace(string powertoolsNamespace)
        {
            if (powertoolsNamespace != null)
            {
                _namespace = powertoolsNamespace;
            }
        }

        public static void SetService(string powertoolsService)
        {
            if (powertoolsService != null)
            {
                _service = powertoolsService;
            }
        }
    }
}