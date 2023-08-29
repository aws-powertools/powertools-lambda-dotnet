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
    /// <param name="inProgressExpiryTimestamp">Unix timestamp of in-progress field for the remaining lambda execution time</param>
    public DataRecord(string idempotencyKey, 
            DataRecordStatus status,
            long expiryTimestamp,
            string responseData,
            string payloadHash, 
            long? inProgressExpiryTimestamp = null)
    {
        IdempotencyKey = idempotencyKey;
        _status = status.ToString();
        ExpiryTimestamp = expiryTimestamp;
        ResponseData = responseData;
        PayloadHash = payloadHash;
        InProgressExpiryTimestamp = inProgressExpiryTimestamp;
    }

    /// <summary>
    /// A hash representation of either the entire event or a specific configured subset of the event
    /// </summary>
    public string IdempotencyKey { get; }
    /// <summary>
    /// Unix timestamp of when record expires.
    /// This field is controlling how long the result of the idempotent
    /// event is cached. It is stored in _seconds since epoch_.
    /// DynamoDB's TTL mechanism is used to remove the record once the
    /// expiry has been reached, and subsequent execution of the request
    /// will be permitted. The user must configure this on their table.
    /// </summary>
    public long ExpiryTimestamp { get; }

    /// <summary>
    /// The in-progress field is set to the remaining lambda execution time
    /// when the record is created.
    /// This field is stored in _milliseconds since epoch_.
    /// 
    /// This ensures that:
    ///     1/ other concurrently executing requests are blocked from starting
    ///     2/ if a lambda times out, subsequent requests will be allowed again, despite
    ///        the fact that the idempotency record is already in the table
    /// </summary>
    public long? InProgressExpiryTimestamp { get; }

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
    public DataRecordStatus Status => 
        IsExpired(DateTimeOffset.UtcNow) ? DataRecordStatus.EXPIRED : Enum.Parse<DataRecordStatus>(_status);

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