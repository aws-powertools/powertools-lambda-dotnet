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

using Amazon;
using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;
using Amazon.Runtime;
using AWS.Lambda.Powertools.Parameters.Internal.AppConfig;
using AWS.Lambda.Powertools.Parameters.Cache;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Internal.Cache;
using AWS.Lambda.Powertools.Parameters.Provider;

namespace AWS.Lambda.Powertools.Parameters.AppConfig;

public class AppConfigProvider : ParameterProvider<AppConfigProviderConfigurationBuilder>, IAppConfigProvider
{
    private string? _defaultApplicationId;
    private string? _defaultEnvironmentId;
    private string? _defaultConfigProfileId;
    private IAmazonAppConfigData? _client;
    private readonly IDateTimeWrapper _dateTimeWrapper;
    private string _pollConfigurationToken = string.Empty;
    private DateTime _nextAllowedPollTime = DateTime.MinValue;
    private IDictionary<string, string?> _lastConfig = new Dictionary<string, string?>();

    private IAmazonAppConfigData Client => _client ??= new AmazonAppConfigDataClient();
    
    protected override ParameterProviderCacheMode CacheMode => 
        ParameterProviderCacheMode.GetMultipleResultOnly;

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
        IDictionary<string, string?>? lastConfig = null)
    {
        _dateTimeWrapper = dateTimeWrapper;
        if (pollConfigurationToken is not null)
            _pollConfigurationToken = pollConfigurationToken;
        if (nextAllowedPollTime is not null)
            _nextAllowedPollTime = nextAllowedPollTime.Value;
        if (lastConfig is not null)
            _lastConfig = lastConfig;
    }

    public IAppConfigProvider UseClient(IAmazonAppConfigData client)
    {
        _client = client;
        return this;
    }

    public IAppConfigProvider ConfigureClient(RegionEndpoint region)
    {
        _client = new AmazonAppConfigDataClient(region);
        return this;
    }

    public IAppConfigProvider ConfigureClient(AmazonAppConfigDataConfig config)
    {
        _client = new AmazonAppConfigDataClient(config);
        return this;
    }

    public IAppConfigProvider ConfigureClient(AWSCredentials credentials)
    {
        _client = new AmazonAppConfigDataClient(credentials);
        return this;
    }

    public IAppConfigProvider ConfigureClient(AWSCredentials credentials, RegionEndpoint region)
    {
        _client = new AmazonAppConfigDataClient(credentials, region);
        return this;
    }

    public IAppConfigProvider ConfigureClient(AWSCredentials credentials, AmazonAppConfigDataConfig config)
    {
        _client = new AmazonAppConfigDataClient(credentials, config);
        return this;
    }

    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey);
        return this;
    }

    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, region);
        return this;
    }

    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey,
        AmazonAppConfigDataConfig config)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, config);
        return this;
    }

    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken);
        return this;
    }

    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        RegionEndpoint region)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, region);
        return this;
    }

    public IAppConfigProvider ConfigureClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken,
        AmazonAppConfigDataConfig config)
    {
        _client = new AmazonAppConfigDataClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config);
        return this;
    }

    public IAppConfigProvider DefaultApplication(string applicationId)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
            throw new ArgumentNullException(nameof(applicationId));
        _defaultApplicationId = applicationId;
        return this;
    }
    
    public IAppConfigProvider DefaultEnvironment(string environmentId)
    {
        if (string.IsNullOrWhiteSpace(environmentId))
            throw new ArgumentNullException(nameof(environmentId));
        _defaultEnvironmentId = environmentId;
        return this;
    }
    
    public IAppConfigProvider DefaultConfigProfile(string configProfileId)
    {
        _defaultConfigProfileId = configProfileId;
        return this;
    }
    
    public AppConfigProviderConfigurationBuilder WithApplication(string applicationId)
    {
        return NewConfigurationBuilder().WithApplication(applicationId);
    }

    public AppConfigProviderConfigurationBuilder WithEnvironment(string environmentId)
    {
        return NewConfigurationBuilder().WithEnvironment(environmentId);
    }

    public AppConfigProviderConfigurationBuilder WithConfigProfile(string configProfileId)
    {
        return NewConfigurationBuilder().WithConfigProfile(configProfileId);
    }

    protected override AppConfigProviderConfigurationBuilder NewConfigurationBuilder()
    {
        return new AppConfigProviderConfigurationBuilder(this);
    }

    public IDictionary<string, string?> Get()
    {
        return GetAsync().GetAwaiter().GetResult();
    }

    public async Task<IDictionary<string, string?>> GetAsync()
    {
        return await GetMultipleAsync(
                AppConfigProviderCacheHelper.GetCacheKey(_defaultApplicationId, _defaultEnvironmentId,
                    _defaultConfigProfileId))
            .ConfigureAwait(false);
    }

    protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        var configuration = GetProviderConfiguration(config, false);
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(configuration);

        var result = !configuration.ForceFetch ? Cache.Get(cacheKey) as IDictionary<string, string?> : null;
        if (result is null)
        {
            result = await GetLastConfigurationAsync(configuration).ConfigureAwait(false);
            Cache.Set(cacheKey, result, GetMaxAge(config));
        }

        return result.TryGetValue(key, out var value) ? value : null;
    }

    protected override async Task<IDictionary<string, string?>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config)
    {
        var configuration = GetProviderConfiguration(config, true);
        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey(configuration);
        
        if (!string.Equals(path, cacheKey, StringComparison.CurrentCultureIgnoreCase))
            throw new NotSupportedException("Impossible to get multiple values from AWS AppConfig");

        return await GetLastConfigurationAsync(configuration)
            .ConfigureAwait(false);
    }

    private AppConfigProviderConfiguration GetProviderConfiguration(ParameterProviderConfiguration? config, bool multipleValues)
    {
        if (config is not AppConfigProviderConfiguration configuration)
        {
            configuration = new AppConfigProviderConfiguration
            {
                ApplicationId = _defaultApplicationId,
                EnvironmentId = _defaultEnvironmentId,
                ConfigProfileId = _defaultConfigProfileId
            };
        }
        else
        {
            if (string.IsNullOrWhiteSpace(configuration.ApplicationId))
                configuration.ApplicationId = _defaultApplicationId;
        
            if (string.IsNullOrWhiteSpace(configuration.EnvironmentId))
                configuration.EnvironmentId = _defaultEnvironmentId;
        
            if (string.IsNullOrWhiteSpace(configuration.ConfigProfileId))
                configuration.ConfigProfileId = _defaultConfigProfileId;
        }

        if (string.IsNullOrWhiteSpace(configuration.ApplicationId))
            throw multipleValues
                ? new NotSupportedException("Impossible to get multiple values from AWS AppConfig")
                : new ArgumentNullException(nameof(configuration.ApplicationId));
        if (string.IsNullOrWhiteSpace(configuration.EnvironmentId))
            throw multipleValues
                ? new NotSupportedException("Impossible to get multiple values from AWS AppConfig")
                : new ArgumentNullException(nameof(configuration.EnvironmentId));
        if (string.IsNullOrWhiteSpace(configuration.ConfigProfileId))
            throw multipleValues
                ? new NotSupportedException("Impossible to get multiple values from AWS AppConfig")
                : new ArgumentNullException(nameof(configuration.EnvironmentId));

        return configuration;
    }
    
    private async Task<IDictionary<string, string?>> GetLastConfigurationAsync(AppConfigProviderConfiguration config)
    {
        if (_dateTimeWrapper.UtcNow < _nextAllowedPollTime)
        {
            if (!config.ForceFetch)
                return _lastConfig;

            _pollConfigurationToken = string.Empty;
            _nextAllowedPollTime = DateTime.MinValue;
        }

        if (string.IsNullOrEmpty(_pollConfigurationToken))
            _pollConfigurationToken =
                await GetInitialConfigurationTokenAsync(config)
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

    private static IDictionary<string, string?> ParseConfig(string contentType, Stream configuration)
    {
        return contentType switch
        {
            "application/json" => JsonConfigurationParser.Parse(configuration),
            _ => throw new NotImplementedException($"Not implemented AppConfig type: {contentType}")
        };
    }
    
}