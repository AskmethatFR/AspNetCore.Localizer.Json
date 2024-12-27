using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using AspNetCore.Localizer.Json.Format;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

internal sealed class CacheHelper
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, string> _serializedMemoryCache;

    public CacheHelper(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
        _serializedMemoryCache = new ConcurrentDictionary<string, string>();
    }

    public CacheHelper(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _serializedMemoryCache = new ConcurrentDictionary<string, string>();
    }

    public bool TryGetValue(string cacheKey, out Dictionary<string, LocalizatedFormat> localization)
    {
        if (_memoryCache != null && _memoryCache.TryGetValue(cacheKey, out localization))
        {
            return true;
        }

        if (_distributedCache != null)
        {
            if (_serializedMemoryCache.TryGetValue(cacheKey, out var json))
            {
                localization = JsonSerializer.Deserialize<Dictionary<string, LocalizatedFormat>>(json);
                return true;
            }

            json = _distributedCache.GetString(cacheKey);
            if (json != null)
            {
                _serializedMemoryCache[cacheKey] = json;
                localization = JsonSerializer.Deserialize<Dictionary<string, LocalizatedFormat>>(json);
                return true;
            }
        }

        localization = null;
        return false;
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

            _serializedMemoryCache[cacheKey] = json; 
            var cacheEntryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(cacheDuration);

            _distributedCache.SetString(cacheKey, json, cacheEntryOptions);
        }
    }

    public void Remove(string cacheKey)
    {
        _memoryCache?.Remove(cacheKey);
        _serializedMemoryCache.TryRemove(cacheKey, out _);

        if (_distributedCache != null)
        {
            _distributedCache.Remove(cacheKey);
        }
    }
}
