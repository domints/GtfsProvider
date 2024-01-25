using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GtfsProvider.Common.Utils
{
    public interface ICreationSafeCache
    {
        Task<T?> GetOrCreateSafeAsync<T>(object key, Func<ICacheEntry, Task<T>> factory);
    }

    public class CreationSafeCache : ICreationSafeCache
    {
        public static ConcurrentDictionary<object, Lazy<SemaphoreSlim>> _semaphores = new();

        private readonly MemoryCache _internalCache;

        public CreationSafeCache(IOptions<MemoryCacheOptions> optionsAccessor)
        {
            _internalCache = new MemoryCache(optionsAccessor);
        }

        public CreationSafeCache(IOptions<MemoryCacheOptions> optionsAccessor, ILoggerFactory loggerFactory)
        {
            _internalCache = new MemoryCache(optionsAccessor, loggerFactory);
        }

        public Task<T?> GetOrCreateSafeAsync<T>(object key, Func<ICacheEntry, Task<T>> factory)
        {
            return _internalCache.GetOrCreateAsync(key, async cacheEntry =>
            {
                var sem = GetSemaphoreForKey(key);
                await sem.WaitAsync();
                T? value;
                try
                {
                    value = await factory(cacheEntry);
                }
                finally
                {
                    sem.Release();
                }
                return value;
            });
        }

        private SemaphoreSlim GetSemaphoreForKey(object key)
        {
            var semaphoreLazy = _semaphores.GetOrAdd(key, _ => new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1)));
            return semaphoreLazy.Value;
        }
    }
}