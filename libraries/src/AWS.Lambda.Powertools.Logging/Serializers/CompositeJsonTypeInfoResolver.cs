#if NET8_0_OR_GREATER

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AWS.Lambda.Powertools.Logging.Serializers
{
    /// <summary>
    /// Combines multiple IJsonTypeInfoResolver instances into one
    /// </summary>
    internal class CompositeJsonTypeInfoResolver : IJsonTypeInfoResolver
    {
        private readonly IJsonTypeInfoResolver[] _resolvers;

        /// <summary>
        /// Creates a new composite resolver from multiple resolvers
        /// </summary>
        /// <param name="resolvers">Array of resolvers to use</param>
        public CompositeJsonTypeInfoResolver(IJsonTypeInfoResolver[] resolvers)
        {
            _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
        }


        /// <summary>
        /// Gets JSON type info by trying each resolver in order (.NET Standard 2.0 version)
        /// </summary>
        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            foreach (var resolver in _resolvers)
            {
                var typeInfo = resolver?.GetTypeInfo(type, options);
                if (typeInfo != null)
                {
                    return typeInfo;
                }
            }

            return null;
        }
    }
}
#endif