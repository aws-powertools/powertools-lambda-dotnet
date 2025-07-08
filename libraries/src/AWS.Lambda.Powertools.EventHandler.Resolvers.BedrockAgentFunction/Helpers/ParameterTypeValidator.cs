namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers
{
    /// <summary>
    /// Validates parameter types for Bedrock Agent functions
    /// </summary>
    internal class ParameterTypeValidator
    {
        private static readonly HashSet<Type> BedrockParameterTypes = new()
        {
            typeof(string),
            typeof(int),
            typeof(long),
            typeof(double),
            typeof(bool),
            typeof(decimal),
            typeof(DateTime),
            typeof(Guid),
            typeof(string[]),
            typeof(int[]),
            typeof(long[]),
            typeof(double[]),
            typeof(bool[]),
            typeof(decimal[])
        };

        /// <summary>
        /// Checks if a type is a valid Bedrock parameter type
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is valid for Bedrock parameters</returns>
        public bool IsBedrockParameter(Type type) =>
            BedrockParameterTypes.Contains(type) || type.IsEnum ||
            (type.IsArray && BedrockParameterTypes.Contains(type.GetElementType()!));
    }
}
