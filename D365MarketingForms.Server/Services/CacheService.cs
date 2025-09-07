using Microsoft.Extensions.Caching.Memory;

namespace D365MarketingForms.Server.Services
{
    public interface ICacheService
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan duration);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan duration);
        void Remove(string key);
    }

    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return value;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return default;
        }

        public void Set<T>(string key, T value, TimeSpan duration)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration,
                SlidingExpiration = TimeSpan.FromMinutes(Math.Min(duration.TotalMinutes / 2, 10))
            };

            _cache.Set(key, value, options);
            _logger.LogDebug("Added to cache: {Key} (expires in {Duration})", key, duration);
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan duration)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return value!;
            }

            _logger.LogDebug("Cache miss for key: {Key}, fetching data", key);
            value = await factory();

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration,
                SlidingExpiration = TimeSpan.FromMinutes(Math.Min(duration.TotalMinutes / 2, 10))
            };

            _cache.Set(key, value, options);
            _logger.LogDebug("Added to cache: {Key} (expires in {Duration})", key, duration);

            return value;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed from cache: {Key}", key);
        }
    }
}