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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Idempotency.Exceptions;
using AWS.Lambda.Powertools.Idempotency.Internal;
using AWS.Lambda.Powertools.Idempotency.Output;
using AWS.Lambda.Powertools.Idempotency.Serialization;
using DevLab.JmesPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWS.Lambda.Powertools.Idempotency.Persistence;

/// <summary>
/// Persistence layer that will store the idempotency result.
/// Base implementation. See <see cref="DynamoDBPersistenceStore"/> for an implementation (default one)
/// Extends this class to use your own implementation (DocumentDB, Elasticache, ...)
/// </summary>
public abstract class BasePersistenceStore : IPersistenceStore
{
    private IdempotencyOptions _idempotencyOptions = null!;
    private string? _functionName;
    /// <summary>
    /// Boolean to indicate whether or not payload validation is enabled
    /// </summary>
    protected bool PayloadValidationEnabled;
    private LRUCache<string, DataRecord> _cache = null!;
    /// <summary>
    /// Instance of ILog to log the internal details of idempotency
    /// </summary>
    protected ILog Log = null!;

    /// <summary>
    /// Initialize the base persistence layer from the configuration settings
    /// </summary>
    /// <param name="idempotencyOptions">Idempotency configuration settings</param>
    /// <param name="functionName">The name of the function being decorated</param>
    public void Configure(IdempotencyOptions idempotencyOptions, string? functionName)
    {
        string? funcEnv = Environment.GetEnvironmentVariable(Constants.LambdaFunctionNameEnv);
        _functionName = funcEnv != null ? funcEnv : "testFunction";
        if (!string.IsNullOrWhiteSpace(functionName))
        {
            _functionName += "." + functionName;
        }
        _idempotencyOptions = idempotencyOptions;
        Log = _idempotencyOptions.Log;

        //TODO: optimize to not reconfigure
        if (!string.IsNullOrWhiteSpace(_idempotencyOptions.PayloadValidationJmesPath))
        {
            PayloadValidationEnabled = true;
        }
        
        var useLocalCache = _idempotencyOptions.UseLocalCache;
        if (useLocalCache)
        {
            _cache = new (_idempotencyOptions.LocalCacheMaxItems);
        }
    }
    
    /// <summary>
    /// For test purpose only (adding a cache to mock)
    /// </summary>
    internal void Configure(IdempotencyOptions options, string functionName, LRUCache<string, DataRecord> cache)
    {
        Configure(options, functionName);
        _cache = cache;
    }

    /// <summary>
    /// Save record of function's execution completing successfully
    /// </summary>
    /// <param name="data">Payload</param>
    /// <param name="result">the response from the function</param>
    /// <param name="now">The current date time</param>
    public virtual async Task SaveSuccess(JToken data, object result, DateTimeOffset now)
    {
        string responseJson = JsonConvert.SerializeObject(result);
        var record = new DataRecord(
            GetHashedIdempotencyKey(data),
            DataRecord.DataRecordStatus.COMPLETED,
            GetExpiryEpochSecond(now),
            responseJson,
            GetHashedPayload(data)
        );
        Log.WriteDebug("Function successfully executed. Saving record to persistence store with idempotency key: {0}", record.IdempotencyKey);
        await UpdateRecord(record);
        SaveToCache(record);
    }

    /// <summary>
    /// Save record of function's execution being in progress
    /// </summary>
    /// <param name="data">Payload</param>
    /// <param name="now">The current date time</param>
    /// <exception cref="IdempotencyItemAlreadyExistsException"></exception>
    public virtual async Task SaveInProgress(JToken data, DateTimeOffset now)
    {
        string idempotencyKey = GetHashedIdempotencyKey(data);
        
        if (RetrieveFromCache(idempotencyKey, now) != null)
        {
            throw new IdempotencyItemAlreadyExistsException();
        }

        DataRecord record = new DataRecord(
            idempotencyKey,
            DataRecord.DataRecordStatus.INPROGRESS,
            GetExpiryEpochSecond(now),
            null,
            GetHashedPayload(data)
        );
        Log.WriteDebug("saving in progress record for idempotency key: {0}", record.IdempotencyKey);
        await PutRecord(record, now);
    }
    
    /// <summary>
    /// Delete record from the persistence store
    /// </summary>
    /// <param name="data">Payload</param>
    /// <param name="throwable">The throwable thrown by the function</param>
    public virtual async Task DeleteRecord(JToken data, Exception throwable)
    {
        string idemPotencyKey = GetHashedIdempotencyKey(data);

        Log.WriteDebug("Function raised an exception {0}. " +
                  "Clearing in progress record in persistence store for idempotency key: {1}",
            throwable.GetType().Name,
            idemPotencyKey);

        await DeleteRecord(idemPotencyKey);
        DeleteFromCache(idemPotencyKey);
    }
    
    /// <summary>
    /// Retrieve idempotency key for data provided, fetch from persistence store, and convert to DataRecord.
    /// </summary>
    /// <param name="data">Payload</param>
    /// <param name="now"></param>
    /// <returns>DataRecord representation of existing record found in persistence store</returns>
    public virtual async Task<DataRecord> GetRecord(JToken data, DateTimeOffset now)
    {
        string idempotencyKey = GetHashedIdempotencyKey(data);

        DataRecord? cachedRecord = RetrieveFromCache(idempotencyKey, now);
        if (cachedRecord != null)
        {
            Log.WriteDebug("Idempotency record found in cache with idempotency key: {0}", idempotencyKey);
            ValidatePayload(data, cachedRecord);
            return cachedRecord;
        }

        DataRecord record = await GetRecord(idempotencyKey);
        SaveToCache(record);
        ValidatePayload(data, record);
        return record;
    }
    
    /// <summary>
    /// Save data_record to local cache except when status is "INPROGRESS"
    /// NOTE: We can't cache "INPROGRESS" records as we have no way to reflect updates that can happen outside of the
    /// execution environment
    /// </summary>
    /// <param name="dataRecord">DataRecord to save in cache</param>
    private void SaveToCache(DataRecord dataRecord)
    {
        if (!_idempotencyOptions.UseLocalCache)
            return;
        if (dataRecord.Status == DataRecord.DataRecordStatus.INPROGRESS)
            return;

        _cache.Set(dataRecord.IdempotencyKey, dataRecord);
    }
    
    /// <summary>
    /// Validate that the hashed payload matches data provided and stored data record
    /// </summary>
    /// <param name="data">Payload</param>
    /// <param name="dataRecord">DataRecord instance</param>
    /// <exception cref="IdempotencyValidationException"></exception>
    private void ValidatePayload(JToken data, DataRecord dataRecord)
    {
        if (PayloadValidationEnabled)
        {
            string dataHash = GetHashedPayload(data);

            if (dataHash != dataRecord.PayloadHash)
            {
                throw new IdempotencyValidationException("Payload does not match stored record for this event key");
            }
        }
    }
    
    private DataRecord? RetrieveFromCache(string idempotencyKey, DateTimeOffset now)
    {
        if (!_idempotencyOptions.UseLocalCache)
            return null;
        
        if (_cache.TryGet(idempotencyKey, out DataRecord record) && record!=null)
        {
            if (!record.IsExpired(now)) 
            {
                return record;
            }
            Log.WriteDebug("Removing expired local cache record for idempotency key: {0}", idempotencyKey);
            DeleteFromCache(idempotencyKey);
        }
        return null;
    }
    private void DeleteFromCache(string idempotencyKey)
    {
        if (!_idempotencyOptions.UseLocalCache)
            return;
        
        _cache.Delete(idempotencyKey);
    }
    
    /// <summary>
    /// Extract payload using validation key jmespath and return a hashed representation
    /// </summary>
    /// <param name="data">Payload</param>
    /// <returns>Hashed representation of the data extracted by the jmespath expression</returns>
    private string GetHashedPayload(JToken data)
    {
        if (!PayloadValidationEnabled)
        {
            return "";
        }
        
        var jmes = new JmesPath();
        jmes.FunctionRepository.Register<JsonFunction>();
        var result = jmes.Transform(data.ToString(), _idempotencyOptions.PayloadValidationJmesPath);
        var node = JToken.Parse(result);
        return GenerateHash(node);
    }
    
    
    
    /// <summary>
    /// Calculate unix timestamp of expiry date for idempotency record
    /// </summary>
    /// <param name="now"></param>
    /// <returns>unix timestamp of expiry date for idempotency record</returns>
    private long GetExpiryEpochSecond(DateTimeOffset now)
    {
        return now.AddSeconds(_idempotencyOptions.ExpirationInSeconds).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Extract idempotency key and return a hashed representation
    /// </summary>
    /// <param name="data">incoming data</param>
    /// <returns>Hashed representation of the data extracted by the jmespath expression</returns>
    /// <exception cref="IdempotencyKeyException"></exception>
    private string GetHashedIdempotencyKey(JToken data)
    {
        JToken node = data;
        var eventKeyJmesPath = _idempotencyOptions.EventKeyJmesPath;
        if (eventKeyJmesPath != null) 
        {
            var jmes = new JmesPath();
            jmes.FunctionRepository.Register<JsonFunction>();
            var result = jmes.Transform(data.ToString(), eventKeyJmesPath);
            node = JToken.Parse(result);
        }

        if (IsMissingIdemPotencyKey(node))
        {
            if (_idempotencyOptions.ThrowOnNoIdempotencyKey)
            {
                throw new IdempotencyKeyException("No data found to create a hashed idempotency key");
            }
            Log.WriteWarning("No data found to create a hashed idempotency key. JMESPath: {0}", _idempotencyOptions.EventKeyJmesPath);
        }

        string hash = GenerateHash(node);
        return _functionName + "#" + hash;
    }

    private bool IsMissingIdemPotencyKey(JToken data)
    {
        return (data == null) ||
               (data.Type == JTokenType.Array && !data.HasValues) ||
               (data.Type == JTokenType.Object && !data.HasValues) ||
               (data.Type == JTokenType.String && data.ToString() == String.Empty) ||
               (data.Type == JTokenType.Null);
    }
    
    /// <summary>
    /// Generate a hash value from the provided data
    /// </summary>
    /// <param name="data">data to hash</param>
    /// <returns>Hashed representation of the provided data</returns>
    /// <exception cref="ArgumentException"></exception>
    internal string GenerateHash(JToken data)
    {
        JToken node;
        if (data is JObject or JArray) // if array or object, use the json string representation, otherwise get the real value
        {
            node = data;
        }
        else if (data.Type == JTokenType.String)
        {
            node = data.Value<string>();
        }
        else if (data.Type == JTokenType.Integer)
        {
            node = data.Value<long>();
        }
        else if (data.Type == JTokenType.Float)
        {
            node = data.Value<double>();
        }
        else if (data.Type == JTokenType.Boolean)
        {
            node = data.Value<bool>();
        }
        else node = data; // anything else

        
        using var hashAlgorithm = HashAlgorithm.Create(_idempotencyOptions.HashFunction);
        if (hashAlgorithm == null)
        {
            throw new ArgumentException("Invalid HashAlgorithm");
        }
        var stringToHash = node is JValue ? node.ToString() : node.ToString(Formatting.None);
        string hash = GetHash(hashAlgorithm, stringToHash);
        
        return hash;
    }

    private static string GetHash(HashAlgorithm hashAlgorithm, string input)
    {
        // Convert the input string to a byte array and compute the hash.
        byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        
        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        var sBuilder = new StringBuilder();
        
        // Loop through each byte of the hashed data
        // and format each one as a hexadecimal string.
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        
        // Return the hexadecimal string.
        return sBuilder.ToString();
    }

    /// <inheritdoc />
    public abstract Task<DataRecord> GetRecord(string idempotencyKey);

    /// <inheritdoc />
    public abstract Task PutRecord(DataRecord record, DateTimeOffset now);

    /// <inheritdoc />
    public abstract Task UpdateRecord(DataRecord record);

    /// <inheritdoc />
    public abstract Task DeleteRecord(string idempotencyKey);
}