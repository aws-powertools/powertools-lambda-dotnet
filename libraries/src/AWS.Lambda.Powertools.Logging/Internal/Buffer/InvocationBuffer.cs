using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Buffer for a specific invocation
/// </summary>
internal class InvocationBuffer
{
    private readonly ConcurrentQueue<BufferedLogEntry> _buffer = new();
    private int _currentSize;

    public void Add(string logEntry, int maxBytes, int size)
    {
        // If entry size exceeds max buffer size, discard the entry completely
        if (size > maxBytes)
        {
            // Entry is too large to ever fit in buffer, discard it
            return;
        }

        if (_currentSize + size > maxBytes)
        {
            // Remove oldest entries until we have enough space
            while (_currentSize + size > maxBytes && _buffer.TryDequeue(out var removed))
            {
                _currentSize -= removed.Size;
                HasEvictions = true;
            }

            if (_currentSize < 0) _currentSize = 0;
        }

        _buffer.Enqueue(new BufferedLogEntry(logEntry, size));
        _currentSize += size;
    }

    public IReadOnlyCollection<string> GetAndClear()
    {
        var entries = new List<string>();

        try
        {
            while (_buffer.TryDequeue(out var entry))
            {
                entries.Add(entry.Entry);
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
    
    public bool HasEvictions;
}