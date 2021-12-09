using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.PowerTools.Metrics.Serializer
{
    public class StringEnumConverter : JsonConverterFactory
    {
        private readonly JsonNamingPolicy _namingPolicy;
        private readonly bool _allowIntegerValues;
        private readonly JsonStringEnumConverter _baseConverter;

        public StringEnumConverter() : this(null) { }

        private StringEnumConverter(JsonNamingPolicy namingPolicy = null, bool allowIntegerValues = true)
        {
            _namingPolicy = namingPolicy;
            _allowIntegerValues = allowIntegerValues;
            _baseConverter = new JsonStringEnumConverter(namingPolicy, allowIntegerValues);
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return _baseConverter.CanConvert(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var query = from field in typeToConvert.GetFields(BindingFlags.Public | BindingFlags.Static)
                        let attr = field.GetCustomAttribute<EnumMemberAttribute>()
                        where attr != null
                        select (field.Name, attr.Value);
            var dictionary = query.ToDictionary(p => p.Item1, p => p.Item2);
            return dictionary.Count > 0 ? new JsonStringEnumConverter(new DictionaryLookupNamingPolicy(dictionary, _namingPolicy), _allowIntegerValues).CreateConverter(typeToConvert, options) : _baseConverter.CreateConverter(typeToConvert, options);
        }
    }

    public class JsonNamingPolicyDecorator : JsonNamingPolicy
    {
        private readonly JsonNamingPolicy _underlyingNamingPolicy;

        protected JsonNamingPolicyDecorator(JsonNamingPolicy underlyingNamingPolicy) => _underlyingNamingPolicy = underlyingNamingPolicy;

        public override string ConvertName(string name) => _underlyingNamingPolicy == null ? name : _underlyingNamingPolicy.ConvertName(name);
    }

    internal class DictionaryLookupNamingPolicy : JsonNamingPolicyDecorator
    {
        private readonly Dictionary<string, string> _dictionary;

        public DictionaryLookupNamingPolicy(Dictionary<string, string> dictionary, JsonNamingPolicy underlyingNamingPolicy) : base(underlyingNamingPolicy) => _dictionary = dictionary ?? throw new ArgumentNullException();

        public override string ConvertName(string name) => _dictionary.TryGetValue(name, out var value) ? value : base.ConvertName(name);
    }
}
