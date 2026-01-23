using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using AspNetCore.Localizer.Json.Format;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

internal sealed class CacheHelper
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, CacheEntry> _serializedMemoryCache;
    private readonly LinkedList<string> _lruList = new();
    private readonly ReaderWriterLockSlim _lruLock = new();
    private readonly int _maxCacheSize;

    private class CacheEntry
    {
        public string Value { get; set; }
        public DateTime LastAccessTime { get; set; }
        public LinkedListNode<string> LruNode { get; set; }
    }

    public CacheHelper(IDistributedCache distributedCache, int maxCacheSize = 1000)
    {
        _distributedCache = distributedCache;
        _serializedMemoryCache = new ConcurrentDictionary<string, CacheEntry>();
        _maxCacheSize = maxCacheSize;
    }

    public CacheHelper(IMemoryCache memoryCache, int maxCacheSize = 1000)
    {
        _memoryCache = memoryCache;
        _serializedMemoryCache = new ConcurrentDictionary<string, CacheEntry>();
        _maxCacheSize = maxCacheSize;
    }

    public bool TryGetValue(string cacheKey, out Dictionary<string, LocalizatedFormat> localization)
    {
        if (_memoryCache != null && _memoryCache.TryGetValue(cacheKey, out localization))
        {
            return true;
        }

        if (_distributedCache != null)
        {
            if (_serializedMemoryCache.TryGetValue(cacheKey, out var entry))
            {
                UpdateLruAccess(cacheKey, entry);
                localization = JsonSerializer.Deserialize<Dictionary<string, LocalizatedFormat>>(entry.Value);
                return true;
            }

            var json = _distributedCache.GetString(cacheKey);
            if (json != null)
            {
                var newEntry = new CacheEntry { Value = json };
                _serializedMemoryCache[cacheKey] = newEntry;
                UpdateLruAccess(cacheKey, newEntry);
                EvictIfNeeded();
                localization = JsonSerializer.Deserialize<Dictionary<string, LocalizatedFormat>>(json);
                return true;
            }
        }

        localization = null;
        return false;
    }

    public void Dispose()
    {
        _lruLock?.Dispose();
    }

    public void Set(string cacheKey, Dictionary<string, LocalizatedFormat> localization, TimeSpan cacheDuration)
    {
        if (_memoryCache == null && _distributedCache == null)
        {
            throw new InvalidOperationException("Both MemoryCache and DistributedCache are null");
        }

        if (_memoryCache != null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(cacheDuration);

            _memoryCache.Set(cacheKey, localization, cacheEntryOptions);
        }

        if (_distributedCache != null)
        {
            var json = JsonSerializer.Serialize(localization);

            var entry = new CacheEntry { Value = json };
            _serializedMemoryCache[cacheKey] = entry;
            UpdateLruAccess(cacheKey, entry);
            EvictIfNeeded();

            var cacheEntryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(cacheDuration);

            _distributedCache.SetString(cacheKey, json, cacheEntryOptions);
        }
    }

    public void Remove(string cacheKey)
    {
        _memoryCache?.Remove(cacheKey);
        
        if (_serializedMemoryCache.TryRemove(cacheKey, out var entry))
        {
            _lruLock.EnterWriteLock();
            try
            {
                if (entry.LruNode != null)
                {
                    _lruList.Remove(entry.LruNode);
                }
            }
            finally
            {
                _lruLock.ExitWriteLock();
            }
        }

        if (_distributedCache != null)
        {
            _distributedCache.Remove(cacheKey);
        }
    }

    private void UpdateLruAccess(string key, CacheEntry entry)
    {
        _lruLock.EnterWriteLock();
        try
        {
            if (entry.LruNode != null)
            {
                _lruList.Remove(entry.LruNode);
            }

            var node = _lruList.AddLast(key);
            entry.LruNode = node;
            entry.LastAccessTime = DateTime.UtcNow;
        }
        finally
        {
            _lruLock.ExitWriteLock();
        }
    }

    private void EvictIfNeeded()
    {
        if (_serializedMemoryCache.Count >= _maxCacheSize)
        {
            _lruLock.EnterWriteLock();
            try
            {
                if (_lruList.First != null && _serializedMemoryCache.Count >= _maxCacheSize)
                {
                    var lruKey = _lruList.First.Value;
                    _serializedMemoryCache.TryRemove(lruKey, out _);
                    _lruList.RemoveFirst();
                }
            }
            finally
            {
                _lruLock.ExitWriteLock();
            }
        }
    }
}
