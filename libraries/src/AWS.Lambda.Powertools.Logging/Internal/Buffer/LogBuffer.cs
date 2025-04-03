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
using System.Collections.Concurrent;
using System.Collections.Generic;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// A buffer for storing log entries, with isolation per Lambda invocation
/// </summary>
internal class LogBuffer
{
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

// Dictionary of buffers by invocation ID
    private readonly ConcurrentDictionary<string, InvocationBuffer> _buffersByInvocation = new();
    
    // Get the current invocation ID or create a fallback
    private string CurrentInvocationId => _powertoolsConfigurations.XRayTraceId;

    public LogBuffer(IPowertoolsConfigurations powertoolsConfigurations)
    {
        _powertoolsConfigurations = powertoolsConfigurations;
    }
    
    /// <summary>
    /// Add a log entry to the buffer for the current invocation
    /// </summary>
    public void Add(string logEntry, int maxBytes, int size)
    {
        var invocationId = CurrentInvocationId;
        if (string.IsNullOrEmpty(invocationId))
        {
            // No invocation ID set, do not buffer
            return;
        }
        var buffer = _buffersByInvocation.GetOrAdd(invocationId, _ => new InvocationBuffer());
        buffer.Add(logEntry, maxBytes, size);
    }
    
    /// <summary>
    /// Get all entries for the current invocation and clear that buffer
    /// </summary>
    public IReadOnlyCollection<string> GetAndClear()
    {
        var invocationId = CurrentInvocationId;
        
        if (string.IsNullOrEmpty(invocationId))
        {
            // No invocation ID set, return empty
            return Array.Empty<string>();
        }
        
        // Try to get and remove the buffer for this invocation
        if (_buffersByInvocation.TryRemove(invocationId, out var buffer))
        {
            return buffer.GetAndClear();
        }
        
        return Array.Empty<string>();
    }
    
    /// <summary>
    /// Clear all buffers
    /// </summary>
    public void Clear()
    {
        _buffersByInvocation.Clear();
    }
    
    /// <summary>
    /// Clear buffer for the current invocation
    /// </summary>
    public void ClearCurrentInvocation()
    {
        var invocationId = CurrentInvocationId;
        if (_buffersByInvocation.TryRemove(invocationId, out _)) {}
    }
    
    /// <summary>
    /// Check if the current invocation has any buffered entries
    /// </summary>
    public bool HasEntries
    {
        get
        {
            var invocationId = CurrentInvocationId;
            return _buffersByInvocation.TryGetValue(invocationId, out var buffer) && buffer.HasEntries;
        }
    }
    
    public bool HasEvictions
    {
        get
        {
            var invocationId = CurrentInvocationId;
            return _buffersByInvocation.TryGetValue(invocationId, out var buffer) && buffer.HasEvictions;
        }
    }
}