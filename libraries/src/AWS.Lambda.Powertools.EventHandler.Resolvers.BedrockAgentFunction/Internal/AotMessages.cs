namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal;

/// <summary>
/// Messages for the Native AOT annotations on tool registration APIs that rely on reflection.
/// </summary>
internal static class AotMessages
{
    internal const string DelegateTool =
        "Tools registered with a Delegate read the delegate's method and parameters with reflection, which isn't " +
        "supported under Native AOT. Define the tool as a static method of a [BedrockFunctionType] class and " +
        "register it with RegisterTool<T>(), or use a Tool overload that takes a Func<BedrockFunctionRequest, ...> " +
        "or a Func<string>.";
}
