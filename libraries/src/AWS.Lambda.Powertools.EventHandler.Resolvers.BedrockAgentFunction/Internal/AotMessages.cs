namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal;

/// <summary>
/// Messages for the Native AOT annotations on tool registration APIs that rely on reflection.
/// </summary>
internal static class AotMessages
{
    internal const string DelegateTool =
        "Tools registered with a Delegate read the delegate's method and parameters with reflection, which isn't " +
        "supported under Native AOT. Use a Tool overload that takes a Func<BedrockFunctionRequest, ILambdaContext?, " +
        "BedrockFunctionResponse>, Func<BedrockFunctionRequest, BedrockFunctionResponse>, Func<BedrockFunctionResponse> " +
        "or Func<string> instead.";

    internal const string RegisterTool =
        "RegisterTool<T> creates delegates for the tool methods at runtime, which isn't supported under Native AOT. " +
        "Register each tool with a Tool overload that takes a Func<BedrockFunctionRequest, ...> instead.";
}
