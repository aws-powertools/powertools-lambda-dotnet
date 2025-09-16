using System.Runtime.CompilerServices;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal
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