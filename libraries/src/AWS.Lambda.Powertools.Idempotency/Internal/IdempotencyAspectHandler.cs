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
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Internal.Serializers;
using AWS.Lambda.Powertools.Idempotency.Persistence;

namespace AWS.Lambda.Powertools.Idempotency.Internal;

internal class IdempotencyAspectHandler<T>
{
    /// <summary>
    /// Max retries
    /// </summary>
    private const int MaxRetries = 2;
    /// <summary>
    /// Delegate to execute the calling handler
    /// </summary>
    private readonly Func<Task<T>> _target;
    /// <summary>
    /// Request payload
    /// </summary>
    private readonly JsonDocument _data;

    private readonly ILambdaContext _lambdaContext;

    /// <summary>
    /// Persistence store
    /// </summary>
    private readonly BasePersistenceStore _persistenceStore;

    /// <summary>
    /// IdempotencyAspectHandler constructor
    /// </summary>
    /// <param name="target"></param>
    /// <param name="functionName"></param>
    /// <param name="keyPrefix"></param>
    /// <param name="payload"></param>
    /// <param name="lambdaContext"></param>
    public IdempotencyAspectHandler(
        Func<Task<T>> target,
        string functionName,
        string keyPrefix,
        JsonDocument payload,
        ILambdaContext lambdaContext)
    {
        _target = target;
        _data = payload;
        _lambdaContext = lambdaContext;
        _persistenceStore = Idempotency.Instance.PersistenceStore;
        _persistenceStore.Configure(Idempotency.Instance.IdempotencyOptions, functionName, keyPrefix);
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
        for (var i = 0;; i++)
        {
            try
            {
                var processIdempotency = await ProcessIdempotency();
                return processIdempotency;
            }
            catch (IdempotencyInconsistentStateException)
            {
                if (i == MaxRetries)
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
            await _persistenceStore.SaveInProgress(_data, DateTimeOffset.UtcNow, GetRemainingTimeInMillis());
        }
        catch (IdempotencyItemAlreadyExistsException)
        {
            var record = await GetIdempotencyRecord();
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
            Console.WriteLine("An existing idempotency record was deleted before we could fetch it");
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
        switch (record.Status)
        {
            // This code path will only be triggered if the record becomes expired between the saveInProgress call and here
            case DataRecord.DataRecordStatus.EXPIRED:
                throw new IdempotencyInconsistentStateException("saveInProgress and getRecord return inconsistent results");
            case DataRecord.DataRecordStatus.INPROGRESS:
                if (record.InProgressExpiryTimestamp.HasValue && record.InProgressExpiryTimestamp.Value < DateTimeOffset.Now.ToUnixTimeMilliseconds())
                {
                    throw new IdempotencyInconsistentStateException("Item should have been expired in-progress because it already time-outed.");
                }
                throw new IdempotencyAlreadyInProgressException("Execution already in progress with idempotency key: " +
                                                                record.IdempotencyKey);
            case DataRecord.DataRecordStatus.COMPLETED:
            default:
                try
                {
                    var result = IdempotencySerializer.Deserialize<T>(record.ResponseData!);
                    if (result is null)
                    {
                        throw new IdempotencyPersistenceLayerException("Unable to cast function response as " + typeof(T).Name);
                    }
                    return Task.FromResult(result);
                }
                catch (Exception e)
                {
                    throw new IdempotencyPersistenceLayerException("Unable to get function response as " + typeof(T).Name, e);
                }
        }
    }

    /// <summary>
    /// Get the function's response and save it to the persistence layer
    /// </summary>
    /// <returns>Result from Handler delegate</returns>
    /// <exception cref="IdempotencyPersistenceLayerException"></exception>
    private async Task<T> GetFunctionResponse()
    {
        T response;
        try
        {
            response = await _target();
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
            await _persistenceStore.SaveSuccess(_data, response!, DateTimeOffset.UtcNow);
        }
        catch (Exception e)
        {
            throw new IdempotencyPersistenceLayerException(
                "Failed to update record state to success in idempotency store. If you believe this is a powertools bug, please open an issue.",
                e);
        }

        return response;
    }
    
    /// <summary>
    /// Tries to determine the remaining time available for the current lambda invocation.
    /// Currently, it only works if the idempotent handler decorator is used or using {Idempotency#registerLambdaContext(Context)}
    /// </summary>
    /// <returns>the remaining time in milliseconds or empty if the context was not provided/found</returns>
    private double? GetRemainingTimeInMillis() {
        if (_lambdaContext != null) {
            // why TotalMilliseconds? Because it must be the complete duration of the timespan expressed in milliseconds
            return _lambdaContext.RemainingTime.TotalMilliseconds;
        }
        return null;
    }
}