using System.Runtime.CompilerServices;

namespace AWS.Lambda.Powertools.Logging.Internal
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