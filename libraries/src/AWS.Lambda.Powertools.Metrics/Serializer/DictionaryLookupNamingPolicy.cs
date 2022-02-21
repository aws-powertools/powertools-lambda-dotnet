using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AWS.Lambda.Powertools.Metrics;

internal class DictionaryLookupNamingPolicy : JsonNamingPolicyDecorator
{
    private readonly Dictionary<string, string> _dictionary;

    public DictionaryLookupNamingPolicy(Dictionary<string, string> dictionary, JsonNamingPolicy underlyingNamingPolicy)
        : base(underlyingNamingPolicy)
    {
        _dictionary = dictionary ?? throw new ArgumentNullException();
    }

    public override string ConvertName(string name)
    {
        return _dictionary.TryGetValue(name, out var value) ? value : base.ConvertName(name);
    }
}