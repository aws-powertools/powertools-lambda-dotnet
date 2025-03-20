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
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// A simplified buffer for storing log entries
/// </summary>
internal class LogBuffer
{
    // Simple storage for buffered messages
    private readonly ConcurrentQueue<string> _buffer = new();
    
    // Keep track of approximate buffer size
    private int _currentSize = 0;
    
    /// <summary>
    /// Add a log entry to the buffer
    /// </summary>
    public void Add(string logEntry, int maxBytes)
    {
        // Estimate size (very roughly)
        var size = 100 + (logEntry?.Length ?? 0) * 2;
        
        // Check if buffer is full - drop oldest entries until we have space
        if (_currentSize + size > maxBytes)
        {
            while (_buffer.TryDequeue(out _) && _currentSize + size > maxBytes)
            {
                _currentSize -= 100; // Rough size per entry
            }
            
            // Safety check - don't allow negative sizes
            if (_currentSize < 0) _currentSize = 0;
        }
        
        // Add to buffer
        _buffer.Enqueue(logEntry);
        _currentSize += size;
    }
    
    /// <summary>
    /// Get all entries and clear the buffer
    /// </summary>
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
            // If dequeuing fails, just clear and return what we have
            Clear();
        }
        
        _currentSize = 0;
        return entries;
    }
    
    /// <summary>
    /// Clear the buffer without returning entries
    /// </summary>
    public void Clear()
    {
        while (_buffer.TryDequeue(out _)) { }
        _currentSize = 0;
    }
    
    /// <summary>
    /// Check if the buffer has any entries
    /// </summary>
    public bool HasEntries => !_buffer.IsEmpty;
    
    /// <summary>
    /// Get the current estimated size of the buffer
    /// </summary>
    public int CurrentSize => _currentSize;
}