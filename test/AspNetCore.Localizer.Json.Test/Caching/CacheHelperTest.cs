using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.Localizer.Json.Format;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetCore.Localizer.Json.Test.Caching
{
    [TestClass]
    public class CacheHelperTest
    {
        [TestMethod]
        public void Set_And_TryGetValue_MemoryCache_Returns_Stored_Value()
        {
            using var memCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new CacheHelper(memCache);
            var key = "test-key";
            var value = new Dictionary<string, LocalizedFormat>
            {
                ["hello"] = new LocalizedFormat { Value = "world" }
            };

            cache.Set(key, value, TimeSpan.FromMinutes(5));

            var found = cache.TryGetValue(key, out var retrieved);
            Assert.IsTrue(found);
            Assert.AreEqual("world", retrieved["hello"].Value);
        }

        [TestMethod]
        public void TryGetValue_MissingKey_MemoryCache_Returns_False()
        {
            using var memCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new CacheHelper(memCache);

            var found = cache.TryGetValue("nonexistent", out var retrieved);

            Assert.IsFalse(found);
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public void Set_And_TryGetValue_DistributedCache_Returns_Stored_Value()
        {
            var distCache = new FakeDistributedCache();
            var cache = new CacheHelper(distCache);
            var key = "dist-key";
            var value = new Dictionary<string, LocalizedFormat>
            {
                ["foo"] = new LocalizedFormat { Value = "bar" }
            };

            cache.Set(key, value, TimeSpan.FromMinutes(5));

            var found = cache.TryGetValue(key, out var retrieved);
            Assert.IsTrue(found);
            Assert.AreEqual("bar", retrieved["foo"].Value);
        }

        [TestMethod]
        public void TryGetValue_MissingKey_DistributedCache_Returns_False()
        {
            var distCache = new FakeDistributedCache();
            var cache = new CacheHelper(distCache);

            var found = cache.TryGetValue("nonexistent", out var retrieved);

            Assert.IsFalse(found);
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public void Remove_Removes_From_MemoryCache()
        {
            using var memCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new CacheHelper(memCache);
            var key = "remove-key";
            var value = new Dictionary<string, LocalizedFormat>
            {
                ["x"] = new LocalizedFormat { Value = "y" }
            };

            cache.Set(key, value, TimeSpan.FromMinutes(5));
            Assert.IsTrue(cache.TryGetValue(key, out _));

            cache.Remove(key);

            Assert.IsFalse(cache.TryGetValue(key, out _));
        }

        [TestMethod]
        public void Remove_Removes_From_DistributedCache()
        {
            var distCache = new FakeDistributedCache();
            var cache = new CacheHelper(distCache);
            var key = "dist-remove";
            var value = new Dictionary<string, LocalizedFormat>
            {
                ["a"] = new LocalizedFormat { Value = "b" }
            };

            cache.Set(key, value, TimeSpan.FromMinutes(5));
            Assert.IsTrue(cache.TryGetValue(key, out _));

            cache.Remove(key);

            Assert.IsFalse(cache.TryGetValue(key, out _));
        }

        [TestMethod]
        public void Remove_NonExistentKey_Does_Not_Throw()
        {
            using var memCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new CacheHelper(memCache);

            cache.Remove("i-dont-exist");
        }

        [TestMethod]
        public void LRU_Eviction_Evicts_Oldest_When_Over_Limit()
        {
            var distCache = new FakeDistributedCache();
            var cache = new CacheHelper(distCache, maxCacheSize: 3);

            cache.Set("A", MakeEntry("A"), TimeSpan.FromMinutes(5));
            cache.Set("B", MakeEntry("B"), TimeSpan.FromMinutes(5));
            cache.Set("C", MakeEntry("C"), TimeSpan.FromMinutes(5));
            cache.Set("D", MakeEntry("D"), TimeSpan.FromMinutes(5));

            distCache.SimulateDistributedEmpty = true;

            Assert.IsFalse(cache.TryGetValue("A", out _), "A should be evicted (oldest)");
            Assert.IsFalse(cache.TryGetValue("B", out _), "B should be evicted (next)");
            Assert.IsTrue(cache.TryGetValue("C", out _));
            Assert.IsTrue(cache.TryGetValue("D", out _));
        }

        [TestMethod]
        public void LRU_Eviction_Keeps_Recently_Accessed()
        {
            var distCache = new FakeDistributedCache();
            var cache = new CacheHelper(distCache, maxCacheSize: 3);

            cache.Set("A", MakeEntry("A"), TimeSpan.FromMinutes(5));
            cache.Set("B", MakeEntry("B"), TimeSpan.FromMinutes(5));
            cache.Set("C", MakeEntry("C"), TimeSpan.FromMinutes(5));

            cache.TryGetValue("A", out _);

            cache.Set("D", MakeEntry("D"), TimeSpan.FromMinutes(5));

            distCache.SimulateDistributedEmpty = true;

            Assert.IsTrue(cache.TryGetValue("A", out _), "A was accessed, should survive in L2");
            Assert.IsFalse(cache.TryGetValue("B", out _), "B is LRU, should be evicted");
            Assert.IsFalse(cache.TryGetValue("C", out _), "C evicted when D added after A rehydrated");
            Assert.IsTrue(cache.TryGetValue("D", out _));
        }

        [TestMethod]
        public void Concurrent_Access_No_Exception()
        {
            using var memCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new CacheHelper(memCache);
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 50, i =>
            {
                var key = $"key-{i % 10}";
                var value = new Dictionary<string, LocalizedFormat>
                {
                    ["val"] = new LocalizedFormat { Value = $"value-{i}" }
                };

                try
                {
                    cache.Set(key, value, TimeSpan.FromMinutes(5));
                    cache.TryGetValue(key, out _);
                    cache.Remove($"other-{i % 5}");
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            Assert.IsTrue(exceptions.IsEmpty,
                $"Concurrent access threw {exceptions.Count} exception(s): {string.Join("; ", exceptions.Select(e => e.GetType().Name + ": " + e.Message).Distinct())}");
        }

        [TestMethod]
        public void Concurrent_Access_DistributedCache_No_Exception()
        {
            var distCache = new FakeDistributedCache();
            var cache = new CacheHelper(distCache);
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 50, i =>
            {
                var key = $"dist-key-{i % 10}";
                var value = new Dictionary<string, LocalizedFormat>
                {
                    ["val"] = new LocalizedFormat { Value = $"value-{i}" }
                };

                try
                {
                    cache.Set(key, value, TimeSpan.FromMinutes(5));
                    cache.TryGetValue(key, out _);
                    cache.Remove($"dist-other-{i % 5}");
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            Assert.IsTrue(exceptions.IsEmpty,
                $"Concurrent distributed access threw {exceptions.Count} exception(s): {string.Join("; ", exceptions.Select(e => e.GetType().Name + ": " + e.Message).Distinct())}");
        }

        [TestMethod]
        public void DistributedCache_L2_LocalCache_Used_On_Repeat_Access()
        {
            var distCache = new FakeDistributedCache();
            var cache = new CacheHelper(distCache);

            cache.Set("l2", MakeEntry("l2"), TimeSpan.FromMinutes(5));

            distCache.SimulateDistributedEmpty = true;

            var found = cache.TryGetValue("l2", out var retrieved);
            Assert.IsTrue(found, "Should retrieve from L2 serialized memory cache even when distributed fails");
            Assert.AreEqual("l2", retrieved["l2"].Value);
        }

        [TestMethod]
        public void Dispose_Does_Not_Throw()
        {
            var distCache = new FakeDistributedCache();
            var cache = new CacheHelper(distCache);

            cache.Dispose();
        }

        [TestMethod]
        public void MaxCacheSize_Zero_Evicts_Immediately()
        {
            var distCache = new FakeDistributedCache();
            var cache = new CacheHelper(distCache, maxCacheSize: 0);

            cache.Set("first", MakeEntry("first"), TimeSpan.FromMinutes(5));
            cache.Set("second", MakeEntry("second"), TimeSpan.FromMinutes(5));

            distCache.SimulateDistributedEmpty = true;

            Assert.IsFalse(cache.TryGetValue("first", out _));
            Assert.IsFalse(cache.TryGetValue("second", out _));
        }

        private static Dictionary<string, LocalizedFormat> MakeEntry(string value)
        {
            return new Dictionary<string, LocalizedFormat>
            {
                [value] = new LocalizedFormat { Value = value }
            };
        }
    }

    internal class FakeDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _data = new();

        public bool SimulateDistributedEmpty { get; set; }

        public byte[] Get(string key)
        {
            if (SimulateDistributedEmpty)
                return null;

            return _data.TryGetValue(key, out var val) ? val : null;
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (SimulateDistributedEmpty)
                return Task.FromResult<byte[]>(null);

            return Task.FromResult(Get(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options = null)
        {
            _data[key] = value;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options = null, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
            _data.TryGetValue(key, out _);
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            Refresh(key);
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _data.TryRemove(key, out _);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}