using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace ServiceFabric.Samples.Caching.CacheCore
{
    public interface ICacheService : IService
    {
        Task<bool> KeyExistsAsync(string key);
        Task<bool> KeyDeleteAsync(string key);
        Task<bool> KeysDeleteAsync(string[] keys);
        Task<string> StringGetAsync(string key);
        Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry);
        Task ClearAllAsync();
    }
}
