using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public class RingoMemoryCache : ICache
    {
        private readonly IMemoryCache _cache;

        public RingoMemoryCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<T> Get<T>(string key)
        {
            return Task.FromResult(_cache.Get<T>(key));
        }

        public Task Set(string key, object value, TimeSpan timeSpan)
        {
            _cache.Set(key, value, timeSpan);
            return Task.CompletedTask;
        }
    }
}
