using System.Collections.Concurrent;

namespace AWS.Lambda.Powertools.EventHandler.Internal;

/// <summary>
/// Basic LRU cache implementation
/// </summary>
/// <summary>
/// Simple LRU cache implementation for caching route resolutions
/// </summary>
internal class LRUCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;

    internal class CacheItem
    {
        public TKey Key { get; }
        public TValue Value { get; }

        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    public LRUCache(int capacity)
    {
        _capacity = capacity;
        _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>();
        _lruList = new LinkedList<CacheItem>();
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            // Move to the front of the list (most recently used)
            _lruList.Remove(node);
            _lruList.AddFirst(node);
            value = node.Value.Value;
            return true;
        }

        value = default;
        return false;
    }

    public void Set(TKey key, TValue value)
    {
        if (_cache.TryGetValue(key, out var existingNode))
        {
            _lruList.Remove(existingNode);
            _cache.Remove(key);
        }
        else if (_cache.Count >= _capacity)
        {
            // Remove least recently used item
            var lastNode = _lruList.Last;
            _lruList.RemoveLast();
            if (lastNode != null) _cache.Remove(lastNode.Value.Key);
        }

        var newNode = new LinkedListNode<CacheItem>(new CacheItem(key, value));
        _lruList.AddFirst(newNode);
        _cache[key] = newNode;
    }

    public void Clear()
    {
        _cache.Clear();
        _lruList.Clear();
    }
}