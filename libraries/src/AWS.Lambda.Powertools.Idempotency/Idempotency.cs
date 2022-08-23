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
///     The general configuration for idempotency (optional, see {@link IdempotencyConfig.Builder} methods to see defaults values.
/// Use it before the function handler get called.
/// Example: Idempotency.Config().WithPersistenceStore(...).Configure();
/// </summary>
public class Idempotency 
{
    private IdempotencyConfig _idempotencyConfig;
    private BasePersistenceStore _persistenceStore;

    private Idempotency()
    {
    }

    public IdempotencyConfig GetIdempotencyConfig()
    {
        return _idempotencyConfig;
    }

    public BasePersistenceStore GetPersistenceStore()
    {
        if (_persistenceStore == null)
        {
            throw new NullReferenceException("Persistence Store is null, did you call 'Configure()'?");
        }
        return _persistenceStore;
    }

    private void SetConfig(IdempotencyConfig config)
    {
        _idempotencyConfig = config;
    }

    private void SetPersistenceStore(BasePersistenceStore persistenceStore)
    {
        _persistenceStore = persistenceStore;
    }

    private static class Holder {
        public static readonly Idempotency IdempotencyInstance = new Idempotency();
    }

    public static Idempotency Instance() => Holder.IdempotencyInstance;

    /// <summary>
    /// Acts like a builder that can be used to configure Idempotency
    /// </summary>
    /// <returns></returns>
    public static IdempotencyBuilder Config()
    {
        return new IdempotencyBuilder();
    }

    public class IdempotencyBuilder {

        private IdempotencyConfig _config;
        private BasePersistenceStore _store;
        
        /// <summary>
        /// Use this method after configuring persistence layer (mandatory) and idem potency configuration (optional)
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public void Configure()
        {
            if (_store == null)
            {
                throw new NullReferenceException("Persistence Layer is null, configure one with 'WithPersistenceStore()'");
            }
            if (_config == null)
            {
                _config = IdempotencyConfig.Builder().Build();
            }
            Idempotency.Instance().SetConfig(_config);
            Idempotency.Instance().SetPersistenceStore(_store);
        }

        public IdempotencyBuilder WithPersistenceStore(BasePersistenceStore persistenceStore)
        {
            this._store = persistenceStore;
            return this;
        }

        public IdempotencyBuilder WithConfig(IdempotencyConfig config)
        {
            this._config = config;
            return this;
        }
    }


}
