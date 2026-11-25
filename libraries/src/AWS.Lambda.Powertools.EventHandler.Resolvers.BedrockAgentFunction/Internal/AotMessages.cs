namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Internal;

/// <summary>
/// Messages for the Native AOT annotations on tool registration APIs that rely on reflection.
/// </summary>
internal static class AotMessages
{
    internal const string DelegateTool =
        "Tools registered with a Delegate read the delegate's method and parameters with reflection, which isn't " +
        "supported under Native AOT. Use the Tool overload that takes a typed Func and the names of its parameters " +
        "instead, for example Tool(name, description, (string city, int days) => ..., \"city\", \"days\"), or " +
        "register a [BedrockFunctionType] class with RegisterTool<T>().";
}
