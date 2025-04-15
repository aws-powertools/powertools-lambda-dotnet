using System.Collections.Concurrent;

namespace AWS.Lambda.Powertools.EventHandler.Internal;

/// <summary>
/// Basic LRU cache implementation
/// </summary>
internal class LRUCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly ConcurrentDictionary<TKey, LinkedListNode<LRUCacheItem>> _cache;
    private readonly LinkedList<LRUCacheItem> _lruList;
    private readonly object _lock = new();

    /// <summary>
    /// Initialize LRU cache with specified capacity
    /// </summary>
    public LRUCache(int capacity)
    {
        _capacity = capacity;
        _cache = new ConcurrentDictionary<TKey, LinkedListNode<LRUCacheItem>>();
        _lruList = new LinkedList<LRUCacheItem>();
    }

    /// <summary>
    /// Add or update a key-value pair in the cache
    /// </summary>
    public void Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out LinkedListNode<LRUCacheItem> node))
            {
                // Move existing item to front of list
                _lruList.Remove(node);
                node.Value.Value = value;
                _lruList.AddFirst(node);
            }
            else
            {
                // Trim cache if at capacity
                if (_cache.Count >= _capacity && _lruList.Last != null)
                {
                    _cache.TryRemove(_lruList.Last.Value.Key, out _);
                    _lruList.RemoveLast();
                }

                // Add new item to front
                var cacheItem = new LRUCacheItem { Key = key, Value = value };
                var newNode = new LinkedListNode<LRUCacheItem>(cacheItem);
                _lruList.AddFirst(newNode);
                _cache[key] = newNode;
            }
        }
    }

    /// <summary>
    /// Try to get a value from the cache
    /// </summary>
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            lock (_lock)
            {
                // Move accessed item to front of list
                _lruList.Remove(node);
                _lruList.AddFirst(node);
            }
            value = node.Value.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Get or create a value in the cache
    /// </summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (TryGetValue(key, out var value))
        {
            return value;
        }

        var newValue = valueFactory(key);
        Add(key, newValue);
        return newValue;
    }

    /// <summary>
    /// Helper class for LRU cache items
    /// </summary>
    private class LRUCacheItem
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }
}