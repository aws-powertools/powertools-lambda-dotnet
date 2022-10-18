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

using System.Collections.Concurrent;
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

/// <summary>
/// The AppConfigProvider to retrieve parameter values from a AWS AppConfig.
/// </summary>
public class AppConfigProvider : ParameterProvider<AppConfigProviderConfigurationBuilder>, IAppConfigProvider
{
    private string _defaultApplicationId = string.Empty;
    private string _defaultEnvironmentId = string.Empty;
    private string _defaultConfigProfileId = string.Empty;
    
    private IAmazonAppConfigData? _client;
    private readonly IDateTimeWrapper _dateTimeWrapper;
    private readonly ConcurrentDictionary<string, AppConfigResult> _results = new(StringComparer.OrdinalIgnoreCase);

    private IAmazonAppConfigData Client => _client ??= new AmazonAppConfigDataClient();

    public AppConfigProvider()
    {
        _dateTimeWrapper = DateTimeWrapper.Instance;
    }

    internal AppConfigProvider(
        IDateTimeWrapper dateTimeWrapper,
        string? appConfigResultKey = null,
        AppConfigResult? appConfigResult = null)
    {
        _dateTimeWrapper = dateTimeWrapper;
        if (appConfigResultKey is not null && appConfigResult is not null)
            _results.TryAdd(appConfigResultKey, appConfigResult);
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
        return new AppConfigProviderConfigurationBuilder(this)
            .WithApplication(_defaultApplicationId)
            .WithEnvironment(_defaultEnvironmentId)
            .WithConfigProfile(_defaultConfigProfileId);
        
    }

    /// <summary>
    /// Get parameter transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter transformed value.</returns>
    public override async Task<T?> GetAsync<T>(string key) where T : class
    {
        return await NewConfigurationBuilder()
            .GetAsync<T>(key)
            .ConfigureAwait(false);
    }

    public IDictionary<string, string?> Get()
    {
        return GetAsync().GetAwaiter().GetResult();
    }

    public async Task<IDictionary<string, string?>> GetAsync()
    {
        return await NewConfigurationBuilder()
            .GetAsync()
            .ConfigureAwait(false);
    }
    
    public T? Get<T>() where T: class
    {
        return GetAsync<T>().GetAwaiter().GetResult();
    }
    
    public async Task<T?> GetAsync<T>()  where T: class
    {
        return await NewConfigurationBuilder()
            .GetAsync<T>()
            .ConfigureAwait(false);
    }
    
    protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        if(config is not AppConfigProviderConfiguration configuration)
            throw new ArgumentNullException(nameof(config));

        var cacheKey = AppConfigProviderCacheHelper.GetCacheKey
        (
            configuration.ApplicationId,
            configuration.EnvironmentId,
            configuration.ConfigProfileId
        );

        var result = GetAppConfigResult(cacheKey);
        
        if (_dateTimeWrapper.UtcNow < result.NextAllowedPollTime)
        {
            if (!config.ForceFetch)
                return result.LastConfig;

            result.PollConfigurationToken = string.Empty;
            result.NextAllowedPollTime = DateTime.MinValue;
        }

        if (string.IsNullOrWhiteSpace(result.PollConfigurationToken))
            result.PollConfigurationToken =
                await GetInitialConfigurationTokenAsync(configuration)
                    .ConfigureAwait(false);

        var request = new GetLatestConfigurationRequest
        {
            ConfigurationToken = result.PollConfigurationToken
        };

        var response =
            await Client.GetLatestConfigurationAsync(request)
                .ConfigureAwait(false);

        result.PollConfigurationToken = response.NextPollConfigurationToken;
        result.NextAllowedPollTime = _dateTimeWrapper.UtcNow.AddSeconds(response.NextPollIntervalInSeconds);

        if (!string.Equals(response.ContentType, "application/json", StringComparison.CurrentCultureIgnoreCase))
            throw new NotImplementedException($"Not implemented AppConfig type: {response.ContentType}");

        using (var reader = new StreamReader(response.Configuration))
        {
            result.LastConfig = 
                await reader.ReadToEndAsync()
                    .ConfigureAwait(false);
        }

        return result.LastConfig;
    }

    protected override Task<IDictionary<string, string?>> GetMultipleAsync(string key,
        ParameterProviderConfiguration? config)
    {
        throw new NotSupportedException("Impossible to get multiple values from AWS AppConfig");
    }
    
    private AppConfigResult GetAppConfigResult(string cacheKey)
    {
        if (_results.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;

        cachedResult = new AppConfigResult();
        _results.TryAdd(cacheKey, cachedResult);

        return cachedResult;
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
}