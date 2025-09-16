using System.Runtime.CompilerServices;

namespace AWS.Lambda.Powertools.Kafka.Protobuf.Internal
{
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            EnvWrapper.SetExecutionEnvironment();
        }
    }
}