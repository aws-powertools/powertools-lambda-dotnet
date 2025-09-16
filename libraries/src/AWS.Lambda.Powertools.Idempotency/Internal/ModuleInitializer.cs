using System.Runtime.CompilerServices;

namespace AWS.Lambda.Powertools.Idempotency.Internal
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