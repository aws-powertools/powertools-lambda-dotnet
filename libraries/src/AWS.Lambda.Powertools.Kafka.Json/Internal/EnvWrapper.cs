using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Kafka.Json.Internal
{
    /// <summary>
    ///   Wrapper class to set the execution environment
    /// </summary>
    public static class EnvWrapper
    {
        /// <summary>
        ///  Sets the execution environment
        /// </summary>
        public static void SetExecutionEnvironment()
        {
            PowertoolsEnvironment.Instance.SetExecutionEnvironment(typeof(EnvWrapper), "Kafka.Json");
        }
    }
}