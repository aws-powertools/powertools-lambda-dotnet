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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.DynamoDB;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.SecretsManager;
using Xunit;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;
using NSubstitute;

namespace HelloWorld.Tests
{
    /// <summary>
    /// A class to mock ParameterProviderConfigurationBuilder
    /// </summary>
    public class MockParameterProviderConfigurationBuilder : ParameterProviderConfigurationBuilder
    {
        public MockParameterProviderConfigurationBuilder(ParameterProvider parameterProvider) : base(parameterProvider)
        {
        }

        private readonly IParameterProvider _parameterProvider;

        public MockParameterProviderConfigurationBuilder(IParameterProvider parameterProvider) : base(
            Substitute.For<ParameterProvider>())
        {
            _parameterProvider = parameterProvider;
        }

        public override async Task<T> GetAsync<T>(string key) where T : class
        {
            return await _parameterProvider.GetAsync<T>(key);
        }

        public override async Task<IDictionary<string, T>> GetMultipleAsync<T>(string key) where T : class
        {
            return await _parameterProvider.GetMultipleAsync<T>(key);
        }
    }

    public class ParameterLookupTest
    {
        [Fact]
        public async Task TestGetSingleParameterWithSsmProvider()
        {
            // Arrange
            var parameterProviderType = ParameterProviderType.SsmProvider;
            var parameterLookupMethod = ParameterLookupMethod.Get;
            var parameterName = Guid.NewGuid().ToString("D");
            var parameterValue = Guid.NewGuid().ToString("D");

            Environment.SetEnvironmentVariable(EnvironmentVariableNames.SsmSingleParameterNameVariableName,
                parameterName);

            var provider = Substitute.For<ISsmProvider>();
            provider.GetAsync(parameterName).Returns(parameterValue);

            // Act
            var helper = new ParameterLookupHelper(provider, parameterProviderType);
            var result = await helper.GetSingleParameterWithSsmProvider();

            // Assert
            await provider.Received(1).GetAsync(parameterName);
            Assert.NotNull(result);
            Assert.Equal(parameterProviderType, result.Provider);
            Assert.Equal(parameterLookupMethod, result.Method);
            Assert.Equal(parameterName, result.Key);
            Assert.Equal(parameterValue, result.Value);
        }

        [Fact]
        public async Task TestGetMultipleParametersWithSsmProvider()
        {
            // Arrange
            var parameterProviderType = ParameterProviderType.SsmProvider;
            var parameterLookupMethod = ParameterLookupMethod.GetMultiple;
            var parameterPathPrefix = Guid.NewGuid().ToString("D");
            var parameterValues = new Dictionary<string, string>()
            {
                { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") },
                { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") }
            };

            Environment.SetEnvironmentVariable(EnvironmentVariableNames.SsmMultipleParametersPathPrefixVariableName,
                parameterPathPrefix);

            var provider = Substitute.For<ISsmProvider>();
            provider.GetMultipleAsync(parameterPathPrefix).Returns(parameterValues);

            // Act
            var helper = new ParameterLookupHelper(provider, parameterProviderType);
            var result = await helper.GetMultipleParametersWithSsmProvider();

            // Assert
            await provider.Received(1).GetMultipleAsync(parameterPathPrefix);
            Assert.NotNull(result);
            Assert.Equal(parameterProviderType, result.Provider);
            Assert.Equal(parameterLookupMethod, result.Method);
            Assert.Equal(parameterPathPrefix, result.Key);

            var values = result.Value as IDictionary<string, string>;
            Assert.NotNull(values);
            Assert.Equal(parameterValues.Count, values.Count);
            Assert.Equal(parameterValues.First().Key, values.First().Key);
            Assert.Equal(parameterValues.First().Value, values.First().Value);
            Assert.Equal(parameterValues.Last().Key, values.Last().Key);
            Assert.Equal(parameterValues.Last().Value, values.Last().Value);
        }

        [Fact]
        public async Task TestGetSingleSecretWithSecretsProvider()
        {
            // Arrange
            var parameterProviderType = ParameterProviderType.SecretsProvider;
            var parameterLookupMethod = ParameterLookupMethod.Get;
            var secretName = Guid.NewGuid().ToString("D");
            var secretValue = new SecretRecord
            {
                Username = Guid.NewGuid().ToString("D"),
                Password = Guid.NewGuid().ToString("D")
            };

            Environment.SetEnvironmentVariable(EnvironmentVariableNames.SecretsManagerSecretName, secretName);

            var provider = Substitute.For<ISecretsProvider>();
            var configBuilder = new MockParameterProviderConfigurationBuilder(provider);
            provider.GetAsync<SecretRecord>(secretName).Returns(secretValue);
            provider.WithTransformation(Transformation.Json).Returns(configBuilder);

            // Act
            var helper = new ParameterLookupHelper(provider, parameterProviderType);
            var result = await helper.GetSingleSecretWithSecretsProvider();

            // Assert
            await provider.Received(1).GetAsync<SecretRecord>(secretName);
            provider.Received(1).WithTransformation(Transformation.Json);
            Assert.NotNull(result);
            Assert.Equal(parameterProviderType, result.Provider);
            Assert.Equal(parameterLookupMethod, result.Method);
            Assert.Equal(secretName, result.Key);
            Assert.Equal(secretName, result.Key);

            var value = result.Value as SecretRecord;
            Assert.NotNull(value);
            Assert.Equal(secretValue.Username, value.Username);
            Assert.Equal(secretValue.Password, value.Password);
        }

        [Fact]
        public async Task TestGetSingleParameterWithDynamoDBProvider()
        {
            // Arrange
            var parameterProviderType = ParameterProviderType.DynamoDBProvider;
            var parameterLookupMethod = ParameterLookupMethod.Get;
            var dynamoDbTableName = Guid.NewGuid().ToString("D");
            var dynamoDbHashKey = Guid.NewGuid().ToString("D");
            var parameterValue = Guid.NewGuid().ToString("D");

            Environment.SetEnvironmentVariable(EnvironmentVariableNames.DynamoDBSingleParameterTableName,
                dynamoDbTableName);
            Environment.SetEnvironmentVariable(EnvironmentVariableNames.DynamoDBSingleParameterId, dynamoDbHashKey);

            var provider = Substitute.For<IDynamoDBProvider>();
            provider.GetAsync(dynamoDbHashKey).Returns(parameterValue);
            provider.UseTable(dynamoDbTableName).Returns(provider);

            // Act
            var helper = new ParameterLookupHelper(provider, parameterProviderType);
            var result = await helper.GetSingleParameterWithDynamoDBProvider();

            // Assert
            await provider.Received(1).GetAsync(dynamoDbHashKey);
            provider.Received(1).UseTable(dynamoDbTableName);
            Assert.NotNull(result);
            Assert.Equal(parameterProviderType, result.Provider);
            Assert.Equal(parameterLookupMethod, result.Method);
            Assert.Equal(dynamoDbHashKey, result.Key);
            Assert.Equal(parameterValue, result.Value);
        }

        [Fact]
        public async Task TestGetMultipleParametersWithDynamoDBProvider()
        {
            // Arrange
            var parameterProviderType = ParameterProviderType.DynamoDBProvider;
            var parameterLookupMethod = ParameterLookupMethod.GetMultiple;
            var dynamoDbTableName = Guid.NewGuid().ToString("D");
            var dynamoDbHashKey = Guid.NewGuid().ToString("D");
            var parameterValues = new Dictionary<string, string>()
            {
                { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") },
                { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") }
            };

            Environment.SetEnvironmentVariable(EnvironmentVariableNames.DynamoDBMultipleParametersTableName,
                dynamoDbTableName);
            Environment.SetEnvironmentVariable(EnvironmentVariableNames.DynamoDBMultipleParametersParameterId,
                dynamoDbHashKey);

            var provider = Substitute.For<IDynamoDBProvider>();
            provider.GetMultipleAsync(dynamoDbHashKey).Returns(parameterValues);
            provider.UseTable(dynamoDbTableName).Returns(provider);

            // Act
            var helper = new ParameterLookupHelper(provider, parameterProviderType);
            var result = await helper.GetMultipleParametersWithDynamoDBProvider();

            // Assert
            await provider.Received(1).GetMultipleAsync(dynamoDbHashKey);
            provider.Received(1).UseTable(dynamoDbTableName);
            Assert.NotNull(result);
            Assert.Equal(parameterProviderType, result.Provider);
            Assert.Equal(parameterLookupMethod, result.Method);
            Assert.Equal(dynamoDbHashKey, result.Key);

            var values = result.Value as IDictionary<string, string>;
            Assert.NotNull(values);
            Assert.Equal(parameterValues.Count, values.Count);
            Assert.Equal(parameterValues.First().Key, values.First().Key);
            Assert.Equal(parameterValues.First().Value, values.First().Value);
            Assert.Equal(parameterValues.Last().Key, values.Last().Key);
            Assert.Equal(parameterValues.Last().Value, values.Last().Value);
        }
    }
}