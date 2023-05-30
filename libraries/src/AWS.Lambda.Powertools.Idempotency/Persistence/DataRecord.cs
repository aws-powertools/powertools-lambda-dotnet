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

namespace AWS.Lambda.Powertools.Idempotency.Persistence;

/// <summary>
/// Data Class for idempotency records. This is actually the item that will be stored in the persistence layer.
/// </summary>
public class DataRecord
{
    /// <summary>
    /// Status
    /// </summary>
    private readonly string _status;

    /// <summary>
    /// Creates a new DataRecord
    /// </summary>
    /// <param name="idempotencyKey">Hash representation of either entire event or specific configured subject of the event</param>
    /// <param name="status">The DataRecordStatus</param>
    /// <param name="expiryTimestamp">Unix timestamp of when record expires</param>
    /// <param name="responseData">JSON serialized invocation results</param>
    /// <param name="payloadHash">A hash representation of the entire event</param>
    public DataRecord(string idempotencyKey, 
            DataRecordStatus status,
            long expiryTimestamp,
            string responseData,
            string payloadHash)
    {
        IdempotencyKey = idempotencyKey;
        _status = status.ToString();
        ExpiryTimestamp = expiryTimestamp;
        ResponseData = responseData;
        PayloadHash = payloadHash;
    }

    /// <summary>
    /// A hash representation of either the entire event or a specific configured subset of the event
    /// </summary>
    public string IdempotencyKey { get; }
    /// <summary>
    /// Unix timestamp of when record expires
    /// </summary>
    public long ExpiryTimestamp { get; }
    /// <summary>
    /// JSON serialized invocation results
    /// </summary>
    public string ResponseData { get; }
    /// <summary>
    /// A hash representation of the entire event
    /// </summary>
    public string PayloadHash { get; }
    

    /// <summary>
    /// Check if data record is expired (based on expiration configured in the IdempotencyConfig
    /// </summary>
    /// <param name="now"></param>
    /// <returns>Whether the record is currently expired or not</returns>
    public bool IsExpired(DateTimeOffset now)
    {
        return ExpiryTimestamp != 0 && now.ToUnixTimeSeconds() > ExpiryTimestamp;
    }

    
    /// <summary>
    /// Represents the <see cref="DataRecordStatus"/> Status
    /// </summary>
    public DataRecordStatus Status
    {
        get
        {
            var now = DateTimeOffset.UtcNow;
            if (IsExpired(now))
            {
                return DataRecordStatus.EXPIRED;
            }

            return Enum.Parse<DataRecordStatus>(_status);
        }
    }

    /// <summary>
    /// Determines whether the specified DataRecord is equal to the current DataRecord 
    /// </summary>
    /// <param name="other">The DataRecord to compare with the current object.</param>
    /// <returns>true if the specified DataRecord is equal to the current DataRecord; otherwise, false.</returns>
    private bool Equals(DataRecord other)
    {
        return _status == other._status 
               && IdempotencyKey == other.IdempotencyKey 
               && ExpiryTimestamp == other.ExpiryTimestamp 
               && ResponseData == other.ResponseData 
               && PayloadHash == other.PayloadHash;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((DataRecord) obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(IdempotencyKey, _status, ExpiryTimestamp, ResponseData, PayloadHash);
    }

    /// <summary>
    /// Status of the record:
    /// -- INPROGRESS: record initialized when function starts
    /// -- COMPLETED: record updated with the result of the function when it ends
    /// -- EXPIRED: record expired, idempotency will not happen
    /// </summary>
    public enum DataRecordStatus {
        /// <summary>
        /// record initialized when function starts
        /// </summary>
        // ReSharper disable once InconsistentNaming
        INPROGRESS,
        /// <summary>
        /// record updated with the result of the function when it ends
        /// </summary>
        // ReSharper disable once InconsistentNaming
        COMPLETED,
        /// <summary>
        /// record expired, idempotency will not happen
        /// </summary>
        // ReSharper disable once InconsistentNaming
        EXPIRED
    }
}