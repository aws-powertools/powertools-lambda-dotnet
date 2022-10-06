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
using AWS.Lambda.Powertools.Idempotency.Persistence;

namespace AWS.Lambda.Powertools.Idempotency;

/// <summary>
/// Holds the configuration for idempotency:
///     The persistence layer to use for persisting the request and response of the function (mandatory).
///     The general configurations for idempotency (optional, see {@link IdempotencyConfig.Builder} methods to see defaults values.
/// Use it before the function handler get called.
/// Example: Idempotency.Configure(builder => builder.WithPersistenceStore(...));
/// </summary>
public class Idempotency 
{
    /// <summary>
    /// The general configurations for the idempotency
    /// </summary>
    public IdempotencyOptions IdempotencyOptions { get; private set; } = null!;

    /// <summary>
    /// The persistence layer to use for persisting the request and response of the function
    /// </summary>
    public BasePersistenceStore PersistenceStore { get; private set; } = null!;

    private Idempotency()
    {
    }

    private void SetConfig(IdempotencyOptions options)
    {
        IdempotencyOptions = options;
    }

    private void SetPersistenceStore(BasePersistenceStore persistenceStore)
    {
        PersistenceStore = persistenceStore;
    }

    private static class Holder {
        public static readonly Idempotency IdempotencyInstance = new Idempotency();
    }

    public static Idempotency Instance() => Holder.IdempotencyInstance;

    /// <summary>
    /// Use this method to configure persistence layer (mandatory) and idempotency options (optional)
    /// </summary>
    /// <returns></returns>
    public static void Configure(Action<IdempotencyBuilder> configurationAction)
    {
        var builder = new IdempotencyBuilder();
        configurationAction(builder);
        if (builder.Store == null)
        {
            throw new NullReferenceException("Persistence Layer is null, configure one with 'WithPersistenceStore()'");
        }
        if (builder.Options != null)
        {
            Instance().SetConfig(builder.Options);
        }
        else
        {
            Instance().SetConfig(new IdempotencyOptionsBuilder().Build());
        }
        Instance().SetPersistenceStore(builder.Store);
    }

    /// <summary>
    /// Create a builder that can be used to configure and create <see cref="Idempotency"/>
    /// </summary>
    public class IdempotencyBuilder
    {
        private IdempotencyOptions? _options;
        private BasePersistenceStore? _store;

        internal IdempotencyOptions? Options => _options;
        internal BasePersistenceStore? Store => _store;

        /// <summary>
        /// Set the persistence layer to use for storing the request and response
        /// </summary>
        /// <param name="persistenceStore"></param>
        /// <returns></returns>
        public IdempotencyBuilder WithPersistenceStore(BasePersistenceStore persistenceStore)
        {
            _store = persistenceStore;
            return this;
        }

        /// <summary>
        /// Configure Idempotency to use DynamoDBPersistenceStore
        /// </summary>
        /// <param name="builderAction">The builder being used to configure the <see cref="BasePersistenceStore"/></param>
        /// <returns></returns>
        public IdempotencyBuilder UseDynamoDb(Action<DynamoDBPersistenceStoreBuilder> builderAction)
        {
            DynamoDBPersistenceStoreBuilder builder =
                new DynamoDBPersistenceStoreBuilder();
            builderAction(builder);
            _store = builder.Build();
            return this;
        }

        /// <summary>
        /// Configure Idempotency to use DynamoDBPersistenceStore
        /// </summary>
        /// <param name="tableName">The DynamoDb table name</param>
        /// <returns></returns>
        public IdempotencyBuilder UseDynamoDb(string tableName)
        {
            DynamoDBPersistenceStoreBuilder builder =
                new DynamoDBPersistenceStoreBuilder();
            _store = builder.WithTableName(tableName).Build();
            return this;
        }

        /// <summary>
        /// Set the idempotency configurations
        /// </summary>
        /// <param name="builderAction">The builder being used to configure the <see cref="IdempotencyOptions"/>.</param>
        /// <returns></returns>
        public IdempotencyBuilder WithOptions(Action<IdempotencyOptionsBuilder> builderAction)
        {
            IdempotencyOptionsBuilder builder = new IdempotencyOptionsBuilder();
            builderAction(builder);
            _options = builder.Build();
            return this;
        }

        /// <summary>
        /// Set the default idempotency configurations
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public IdempotencyBuilder WithOptions(IdempotencyOptions options)
        {
            _options = options;
            return this;
        }
    }
}