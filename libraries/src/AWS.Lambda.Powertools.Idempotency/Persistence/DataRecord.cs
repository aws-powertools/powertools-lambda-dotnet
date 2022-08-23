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

namespace AWS.Lambda.Powertools.Idempotency.Persistence;

public class DataRecord
{
    private readonly string _status;

    public DataRecord(string idempotencyKey, 
            DataRecordStatus status,
            long expiryTimestamp,
            string? responseData,
            string? payloadHash)
    {
        IdempotencyKey = idempotencyKey;
        _status = status.ToString();
        ExpiryTimestamp = expiryTimestamp;
        ResponseData = responseData;
        PayloadHash = payloadHash;
    }

    public string IdempotencyKey { get; }
    public long ExpiryTimestamp { get; }
    public string? ResponseData { get; }
    public string? PayloadHash { get; }
    

    /// <summary>
    /// Check if data record is expired (based on expiration configured in the IdempotencyConfig
    /// </summary>
    /// <param name="now"></param>
    /// <returns>Whether the record is currently expired or not</returns>
    public bool IsExpired(DateTimeOffset now)
    {
        return ExpiryTimestamp != 0 && now.ToUnixTimeSeconds() > ExpiryTimestamp;
    }

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

    protected bool Equals(DataRecord other)
    {
        return _status == other._status 
               && IdempotencyKey == other.IdempotencyKey 
               && ExpiryTimestamp == other.ExpiryTimestamp 
               && ResponseData == other.ResponseData 
               && PayloadHash == other.PayloadHash;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DataRecord) obj);
    }

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
        INPROGRESS,
        COMPLETED,
        EXPIRED
    }
}