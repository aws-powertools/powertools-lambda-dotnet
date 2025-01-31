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
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Idempotency.Internal.Serializers;
using AWS.Lambda.Powertools.Idempotency.Persistence;

namespace AWS.Lambda.Powertools.Idempotency;

/// <summary>
/// Holds the configuration for idempotency:
///     The persistence layer to use for persisting the request and response of the function (mandatory).
///     The general configurations for idempotency (optional, see {@link IdempotencyConfig.Builder} methods to see defaults values.
/// Use it before the function handler get called.
/// Example: Idempotency.Configure(builder => builder.WithPersistenceStore(...));
/// </summary>
public sealed class Idempotency
{
    /// <summary>
    /// The general configurations for the idempotency
    /// </summary>
    public IdempotencyOptions IdempotencyOptions { get; private set; } = null!;

    /// <summary>
    /// The persistence layer to use for persisting the request and response of the function
    /// </summary>
    public BasePersistenceStore PersistenceStore { get; private set; } = null!;

    /// <summary>
    /// Idempotency Constructor
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    internal Idempotency(IPowertoolsConfigurations powertoolsConfigurations)
    {
        powertoolsConfigurations.SetExecutionEnvironment(this);
    }

    /// <summary>
    /// Set Idempotency options
    /// </summary>
    /// <param name="options"></param>
    private void SetConfig(IdempotencyOptions options)
    {
        IdempotencyOptions = options;
    }

    /// <summary>
    /// Set Persistence Store
    /// </summary>
    /// <param name="persistenceStore"></param>
    private void SetPersistenceStore(BasePersistenceStore persistenceStore)
    {
        PersistenceStore = persistenceStore;
    }

    /// <summary>
    /// Holds the idempotency Instance:
    /// </summary>
    internal static Idempotency Instance { get; } = new(PowertoolsConfigurations.Instance);

    /// <summary>
    /// Use this method to configure persistence layer (mandatory) and idempotency options (optional)
    /// </summary>
    public static void Configure(Action<IdempotencyBuilder> configurationAction)
    {
        var builder = new IdempotencyBuilder();
        configurationAction(builder);
        if (builder.Store == null)
        {
            throw new NullReferenceException("Persistence Layer is null, configure one with 'WithPersistenceStore()'");
        }

        Instance.SetConfig(builder.Options ?? new IdempotencyOptionsBuilder().Build());
        Instance.SetPersistenceStore(builder.Store);
    }

    /// <summary>
    /// Holds ILambdaContext
    /// </summary>
    public ILambdaContext LambdaContext { get; private set; }

    /// <summary>
    /// Can be used in a method which is not the handler to capture the Lambda context,
    /// to calculate the remaining time before the invocation times out.
    /// </summary>
    /// <param name="context"></param>
    public static void RegisterLambdaContext(ILambdaContext context)
    {
        Instance.LambdaContext = context;
    }

    /// <summary>
    /// Create a builder that can be used to configure and create <see cref="Idempotency"/>
    /// </summary>
    public class IdempotencyBuilder
    {
        /// <summary>
        /// Exposes Idempotency options
        /// </summary>
        internal IdempotencyOptions Options { get; private set; }

        /// <summary>
        /// Exposes Persistence Store
        /// </summary>
        internal BasePersistenceStore Store { get; private set; }

        /// <summary>
        /// Set the persistence layer to use for storing the request and response
        /// </summary>
        /// <param name="persistenceStore"></param>
        /// <returns>IdempotencyBuilder</returns>
        public IdempotencyBuilder WithPersistenceStore(BasePersistenceStore persistenceStore)
        {
            Store = persistenceStore;
            return this;
        }

        /// <summary>
        /// Configure Idempotency to use DynamoDBPersistenceStore
        /// </summary>
        /// <param name="builderAction">The builder being used to configure the <see cref="BasePersistenceStore"/></param>
        /// <returns>IdempotencyBuilder</returns>
        public IdempotencyBuilder UseDynamoDb(Action<DynamoDBPersistenceStoreBuilder> builderAction)
        {
            var builder =
                new DynamoDBPersistenceStoreBuilder();
            builderAction(builder);
            Store = builder.Build();
            return this;
        }

        /// <summary>
        /// Configure Idempotency to use DynamoDBPersistenceStore
        /// </summary>
        /// <param name="tableName">The DynamoDb table name</param>
        /// <returns>IdempotencyBuilder</returns>
        public IdempotencyBuilder UseDynamoDb(string tableName)
        {
            var builder =
                new DynamoDBPersistenceStoreBuilder();
            Store = builder.WithTableName(tableName).Build();
            return this;
        }

        /// <summary>
        /// Set the idempotency configurations
        /// </summary>
        /// <param name="builderAction">The builder being used to configure the <see cref="IdempotencyOptions"/>.</param>
        /// <returns>IdempotencyBuilder</returns>
        public IdempotencyBuilder WithOptions(Action<IdempotencyOptionsBuilder> builderAction)
        {
            var builder = new IdempotencyOptionsBuilder();
            builderAction(builder);
            Options = builder.Build();
            return this;
        }

        /// <summary>
        /// Set the default idempotency configurations
        /// </summary>
        /// <param name="options"></param>
        /// <returns>IdempotencyBuilder</returns>
        public IdempotencyBuilder WithOptions(IdempotencyOptions options)
        {
            Options = options;
            return this;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Set Customer JsonSerializerContext to append to IdempotencySerializationContext 
        /// </summary>
        /// <param name="context"></param>
        /// <returns>IdempotencyBuilder</returns>
        public IdempotencyBuilder WithJsonSerializationContext(JsonSerializerContext context)
        {
            IdempotencySerializer.AddTypeInfoResolver(context);
            return this;
        }
#endif
    }
}