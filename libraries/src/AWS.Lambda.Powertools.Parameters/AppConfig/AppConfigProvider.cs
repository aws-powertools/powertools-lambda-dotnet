/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;
using AWS.Lambda.Powertools.Parameters.Internal.AppConfig;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.AppConfig;

public class AppConfigProvider : ParameterProvider<AppConfigProviderConfigurationBuilder>
{
    private IAmazonAppConfigData? _client;
    private readonly IDateTimeWrapper _dateTimeWrapper;
    
    private string _pollConfigurationToken = string.Empty;
    private DateTime _nextAllowedPollTime = DateTime.MinValue;
    private IDictionary<string, string> _lastConfig = new Dictionary<string, string>();
    private IAmazonAppConfigData Client => _client ??= new AmazonAppConfigDataClient();

    public AppConfigProvider()
    {
        _dateTimeWrapper = DateTimeWrapper.Instance;
    }

    /// <summary>
    /// Constructor for test
    /// </summary>
    /// <param name="dateTimeWrapper"></param>
    /// <param name="pollConfigurationToken"></param>
    /// <param name="nextAllowedPollTime"></param>
    /// <param name="lastConfig"></param>
    internal AppConfigProvider(
        IDateTimeWrapper dateTimeWrapper,
        string? pollConfigurationToken = null,
        DateTime? nextAllowedPollTime = null,
        IDictionary<string, string>? lastConfig = null)
    {
        _dateTimeWrapper = dateTimeWrapper;
        if (pollConfigurationToken is not null)
            _pollConfigurationToken = pollConfigurationToken;
        if (nextAllowedPollTime is not null)
            _nextAllowedPollTime = nextAllowedPollTime.Value;
        if (lastConfig is not null)
            _lastConfig = lastConfig;
    }

    public AppConfigProvider UseClient(IAmazonAppConfigData client)
    {
        _client = client;
        return this;
    }
    
    public AppConfigProviderConfigurationBuilder WithApplicationIdentifier(string applicationId)
    {
        return NewConfigurationBuilder().WithApplicationIdentifier(applicationId);
    }

    public AppConfigProviderConfigurationBuilder WithEnvironmentIdentifier(string environmentId)
    {
        return NewConfigurationBuilder().WithEnvironmentIdentifier(environmentId);
    }
    
    public AppConfigProviderConfigurationBuilder WithConfigurationProfileIdentifier(string configProfileId)
    {
        return NewConfigurationBuilder().WithConfigurationProfileIdentifier(configProfileId);
    }

    protected override AppConfigProviderConfigurationBuilder NewConfigurationBuilder()
    {
        return new AppConfigProviderConfigurationBuilder(this);
    }

    protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        if (config is not AppConfigProviderConfiguration configuration)
            throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrWhiteSpace(configuration.ApplicationId))
            throw new ArgumentNullException(nameof(configuration.ApplicationId));
        if (string.IsNullOrWhiteSpace(configuration.EnvironmentId))
            throw new ArgumentNullException(nameof(configuration.EnvironmentId));
        if (string.IsNullOrWhiteSpace(configuration.ConfigProfileId))
            throw new ArgumentNullException(nameof(configuration.ConfigProfileId));

        var result =
            await GetMultipleAsync(AppConfigProviderCacheHelper.GetCacheKey(configuration), configuration);

        return result.TryGetValue(key, out var value) ? value : null;
    }

    protected override async Task<IDictionary<string, string>> GetMultipleAsync(string path, ParameterProviderConfiguration? config)
    {
        if (config is not AppConfigProviderConfiguration configuration ||
            string.IsNullOrWhiteSpace(configuration.ApplicationId) ||
            string.IsNullOrWhiteSpace(configuration.EnvironmentId) ||
            string.IsNullOrWhiteSpace(configuration.ConfigProfileId))
            throw new NotSupportedException("Impossible to get multiple values from AWS AppConfig");

        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(configuration);
        if (!string.Equals(path, cacheKey, StringComparison.CurrentCultureIgnoreCase))
            throw new NotSupportedException("Impossible to get multiple values from AWS AppConfig");
        
        if (_dateTimeWrapper.UtcNow < _nextAllowedPollTime)
        {
            if (!configuration.ForceFetch)
                return _lastConfig;
            
            _pollConfigurationToken = string.Empty;
            _nextAllowedPollTime = DateTime.MinValue;
        }

        if (string.IsNullOrEmpty(_pollConfigurationToken))
            _pollConfigurationToken = 
                await GetInitialConfigurationTokenAsync(configuration)
                    .ConfigureAwait(false);

        var request = new GetLatestConfigurationRequest
        {
            ConfigurationToken = _pollConfigurationToken
        };
        
        var response = 
            await Client.GetLatestConfigurationAsync(request)
                .ConfigureAwait(false);
        
        _pollConfigurationToken = response.NextPollConfigurationToken;
        _nextAllowedPollTime = _dateTimeWrapper.UtcNow.AddSeconds(response.NextPollIntervalInSeconds);
        
        _lastConfig = ParseConfig(response.ContentType, response.Configuration);
        return _lastConfig;
    }
    
    private async Task<string> GetInitialConfigurationTokenAsync(AppConfigProviderConfiguration config)
    {
        var request = new StartConfigurationSessionRequest
        {
            ApplicationIdentifier = config.ApplicationId,
            EnvironmentIdentifier = config.EnvironmentId,
            ConfigurationProfileIdentifier = config.ConfigProfileId
        };
        
        return (await Client.StartConfigurationSessionAsync(request).ConfigureAwait(false)).InitialConfigurationToken;
    }
    
    private static IDictionary<string, string> ParseConfig(string contentType, Stream configuration)
    {
        return contentType switch
        {
            "application/json" => JsonConfigurationParser.Parse(configuration),
            _ => throw new NotImplementedException($"Not implemented AppConfig type: {contentType}")
        };
    }
}