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
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Output;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWS.Lambda.Powertools.Idempotency.Internal;

public class IdempotencyHandler<T>
{
    private static readonly int MAX_RETRIES = 2;

    private readonly Func<object[], object> _target;
    private readonly object[] _args;
    private readonly JToken _data;
    private readonly BasePersistenceStore _persistenceStore;
    private readonly ILog _log;

    public IdempotencyHandler(
        Func<object[], object> target, 
        object[] args,
        string functionName,
        JToken payload)
    {
        _target = target;
        _args = args;
        _data = payload;
        _persistenceStore = Idempotency.Instance().PersistenceStore;
        _persistenceStore.Configure(Idempotency.Instance().IdempotencyOptions, functionName);
        _log = Idempotency.Instance().IdempotencyOptions.Log;
    }

    /// <summary>
    /// Main entry point for handling idempotent execution of a function.
    /// </summary>
    /// <returns>function response</returns>
    public async Task<T> Handle()
    {
        // IdempotencyInconsistentStateException can happen under rare but expected cases
        // when persistent state changes in the small time between put & get requests.
        // In most cases we can retry successfully on this exception.
        for (int i = 0; true; i++)
        {
            try
            {
                var processIdempotency = await ProcessIdempotency();
                return processIdempotency;
            }
            catch (IdempotencyInconsistentStateException)
            {
                if (i == MAX_RETRIES)
                {
                    throw;
                }
            }
        }
    }
    
    /// <summary>
    /// Process the function with idempotency
    /// </summary>
    /// <returns>function response</returns>
    /// <exception cref="IdempotencyPersistenceLayerException"></exception>
    private async Task<T> ProcessIdempotency()
    {
        
        try
        {
            // We call saveInProgress first as an optimization for the most common case where no idempotent record
            // already exists. If it succeeds, there's no need to call getRecord.
            await _persistenceStore.SaveInProgress(_data, DateTimeOffset.UtcNow);
        }
        catch (IdempotencyItemAlreadyExistsException)
        {
            DataRecord record = await GetIdempotencyRecord();
            return await HandleForStatus(record);
        }
        catch (IdempotencyKeyException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new IdempotencyPersistenceLayerException(
                "Failed to save in progress record to idempotency store. If you believe this is a powertools bug, please open an issue.",
                e);
        }

        var result = await GetFunctionResponse();
        return result;
    }

    /// <summary>
    /// Retrieve the idempotency record from the persistence layer.
    /// </summary>
    /// <returns>the record if available</returns>
    /// <exception cref="IdempotencyInconsistentStateException"></exception>
    /// <exception cref="IdempotencyPersistenceLayerException"></exception>
    private Task<DataRecord> GetIdempotencyRecord()
    {
        try
        {
            return _persistenceStore.GetRecord(_data, DateTimeOffset.UtcNow);
        }
        catch (IdempotencyItemNotFoundException e)
        {
            // This code path will only be triggered if the record is removed between saveInProgress and getRecord
            _log.WriteDebug("An existing idempotency record was deleted before we could fetch it");
            throw new IdempotencyInconsistentStateException("saveInProgress and getRecord return inconsistent results",
                e);
        }
        catch (IdempotencyValidationException)
        {
            throw;
        }
        catch (IdempotencyKeyException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new IdempotencyPersistenceLayerException(
                "Failed to get record from idempotency store. If you believe this is a powertools bug, please open an issue.",
                e);
        }
    }
    
    /// <summary>
    /// Take appropriate action based on data_record's status
    /// </summary>
    /// <param name="record">record DataRecord</param>
    /// <returns>Function's response previously used for this idempotency key, if it has successfully executed already.</returns>
    /// <exception cref="IdempotencyInconsistentStateException"></exception>
    /// <exception cref="IdempotencyAlreadyInProgressException"></exception>
    /// <exception cref="IdempotencyPersistenceLayerException"></exception>
    private Task<T> HandleForStatus(DataRecord record)
    {
        // This code path will only be triggered if the record becomes expired between the saveInProgress call and here
        if (DataRecord.DataRecordStatus.EXPIRED == record.Status)
        {
            throw new IdempotencyInconsistentStateException("saveInProgress and getRecord return inconsistent results");
        }

        if (DataRecord.DataRecordStatus.INPROGRESS == record.Status)
        {
            throw new IdempotencyAlreadyInProgressException("Execution already in progress with idempotency key: " +
                                                            record.IdempotencyKey);
        }

        try
        {
            _log.WriteDebug("Response for key '{0}' retrieved from idempotency store, skipping the function", record.IdempotencyKey);
            var result = JsonConvert.DeserializeObject<T>(record.ResponseData);
            return Task.FromResult(result);
        }
        catch (Exception e)
        {
            throw new IdempotencyPersistenceLayerException("Unable to get function response as " + typeof(T).Name, e);
        }
    }

    private async Task<T> GetFunctionResponse()
    {
        T response;
        try
        {
            response = await (Task<T>)_target(_args);
        }
        catch(Exception handlerException)
        {
            // We need these nested blocks to preserve function's exception in case the persistence store operation
            // also raises an exception
            try
            {
                await _persistenceStore.DeleteRecord(_data, handlerException);
            }
            catch (IdempotencyKeyException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IdempotencyPersistenceLayerException(
                    "Failed to delete record from idempotency store. If you believe this is a powertools bug, please open an issue.",
                    e);
            }

            throw;
        }

        try
        {
            await _persistenceStore.SaveSuccess(_data, response, DateTimeOffset.UtcNow);
        }
        catch (Exception e)
        {
            throw new IdempotencyPersistenceLayerException(
                "Failed to update record state to success in idempotency store. If you believe this is a powertools bug, please open an issue.",
                e);
        }

        return response;
    }
}