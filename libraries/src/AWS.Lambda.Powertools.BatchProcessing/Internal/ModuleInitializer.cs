using System.Runtime.CompilerServices;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal
{
    /// <summary>
    /// Module initializer for AWS Lambda Powertools Batch Processing library.
    /// This class is responsible for setting up the execution environment when the module is loaded.
    /// </summary>
    internal static class ModuleInitializer
    {
        /// <summary>
        /// Initializes the AWS Lambda Powertools Batch Processing module.
        /// This method is automatically called when the module is loaded by the .NET runtime.
        /// It sets the POWERTOOLS_UTILITY environment variable to "BatchProcessing" to enable
        /// proper telemetry and usage tracking for this utility.
        /// </summary>
        /// <remarks>
        /// This method uses the ModuleInitializer attribute to ensure it runs exactly once
        /// when the module containing this code is loaded, before any other code in the module executes.
        /// </remarks>
#pragma warning disable CA2255
        [ModuleInitializer]
#pragma warning restore CA2255
        internal static void Initialize()
        {
            EnvWrapper.SetExecutionEnvironment();
        }
    }
}