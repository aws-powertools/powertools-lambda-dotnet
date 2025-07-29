using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal
{
    public static class EnvWrapper
    {
        public static void SetExecutionEnvironment()
        {
            PowertoolsEnvironment.Instance.SetExecutionEnvironment(typeof(EnvWrapper));
        }
    }
}