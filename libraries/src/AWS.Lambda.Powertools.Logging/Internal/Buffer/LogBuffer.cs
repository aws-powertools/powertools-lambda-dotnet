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
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// A buffer for storing log entries, with isolation per Lambda invocation
/// </summary>
internal class LogBuffer
{
    // Use AsyncLocal for automatic context flow across async calls
    private static readonly AsyncLocal<string> _currentInvocationId = new AsyncLocal<string>();
    
    // Dictionary of buffers by invocation ID
    private readonly ConcurrentDictionary<string, InvocationBuffer> _buffersByInvocation = new();
    
    // Get the current invocation ID or create a fallback
    private string CurrentInvocationId => _currentInvocationId.Value;
    
    /// <summary>
    /// Set the current invocation ID (call this at the start of a Lambda invocation)
    /// </summary>
    public static void SetCurrentInvocationId(string invocationId)
    {
        _currentInvocationId.Value = invocationId;
    }
    
    /// <summary>
    /// Add a log entry to the buffer for the current invocation
    /// </summary>
    public void Add(string logEntry, int maxBytes)
    {
        var invocationId = CurrentInvocationId;
        var buffer = _buffersByInvocation.GetOrAdd(invocationId, _ => new InvocationBuffer());
        buffer.Add(logEntry, maxBytes);
    }
    
    /// <summary>
    /// Get all entries for the current invocation and clear that buffer
    /// </summary>
    public IReadOnlyCollection<string> GetAndClear()
    {
        var invocationId = CurrentInvocationId;
        
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
    
    /// <summary>
    /// Buffer for a specific invocation
    /// </summary>
    private class InvocationBuffer
    {
        private readonly ConcurrentQueue<string> _buffer = new();
        private int _currentSize = 0;
        
        public void Add(string logEntry, int maxBytes)
        {
            // Same implementation as before
            var size = 100 + (logEntry?.Length ?? 0) * 2;
            
            if (_currentSize + size > maxBytes)
            {
                while (_buffer.TryDequeue(out _) && _currentSize + size > maxBytes)
                {
                    _currentSize -= 100;
                }
                
                if (_currentSize < 0) _currentSize = 0;
            }
            
            _buffer.Enqueue(logEntry);
            _currentSize += size;
        }
        
        public IReadOnlyCollection<string> GetAndClear()
        {
            var entries = new List<string>();
            
            try
            {
                while (_buffer.TryDequeue(out var entry))
                {
                    entries.Add(entry);
                }
            }
            catch (Exception)
            {
                _buffer.Clear();
            }
            
            _currentSize = 0;
            return entries;
        }
        
        public bool HasEntries => !_buffer.IsEmpty;
    }
}