using ServiceFabric.Samples.Caching.CacheCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ServiceFabric.Samples.Caching.CacheClient
{
    public sealed class ClusterCacheStrategy<TEntity> : ICacheStrategy<TEntity>
    {
        /// <summary>
        /// The service application name
        /// TODO: Make this generic
        /// </summary>
        private const string CacheServiceUri = "fabric:/Ford.Sdn.CacheApplication/CacheService";

        /// <summary>
        /// The service partition count
        /// </summary>
        private static readonly Dictionary<string, int> ServicePartitionCount = new Dictionary<string, int>();

        /// <summary>
        /// The service proxies
        /// </summary>
        private static readonly Dictionary<string, ICacheService> ServiceProxies = new Dictionary<string, ICacheService>();

        /// <summary>
        /// The concurrent lock
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> ConcurrentLock = new ConcurrentDictionary<Type, object>();

        public ClusterCacheStrategy()
        {
        }

        public bool IsCacheDisabled
        {
            get
            {
                return false;
            }
        }

        #region Async Methods
        public async Task<bool> ClearAsync()
        {
            int partitionId = 0;
            var partitionList = await CacheClientFactory.GetPartitionList(CacheServiceUri).ConfigureAwait(false);
            var clearTasks = partitionList.Select(p => ClearPartitionAsync(++partitionId));
            await Task.WhenAll(clearTasks).ConfigureAwait(false);
            return true;
        }

        public async Task<long> DeleteFromKeyStoreAsync(List<string> keys, string entityName = null)
        {
            var deleteTasks = keys.Select(key => DeleteFromKeyStoreAsync(key, entityName));
            var result = await Task.WhenAll(deleteTasks).ConfigureAwait(false);
            return result.Sum(x => x ? 1 : 0);
        }

        public async Task<bool> DeleteFromKeyStoreAsync(string key, string entityName = null)
        {
            var client = await CacheClientFactory.GetServiceRemotingProxy(CacheServiceUri, key).ConfigureAwait(false);
            return await client.KeyDeleteAsync(GetCacheKey(key, entityName)).ConfigureAwait(false);
        }

        public async Task<bool> DoesExistInKeyStoreAsync(string key, string entityName = null)
        {
            var client = await CacheClientFactory.GetServiceRemotingProxy(CacheServiceUri, key).ConfigureAwait(false);
            return await client.KeyExistsAsync(GetCacheKey(key, entityName)).ConfigureAwait(false);
        }

        public async Task<IEnumerable<TEntity>> GetCollectionFromKeyStoreAsync(string key, string entityName = null)
        {
            var client = await CacheClientFactory.GetServiceRemotingProxy(CacheServiceUri, key).ConfigureAwait(false);
            var result = await client.StringGetAsync(GetCacheKey(key, entityName)).ConfigureAwait(false);
            return Deserialize<IEnumerable<TEntity>>(result);
        }

        public async Task<TEntity> GetFromKeyStoreAsync(string key, string entityName = null)
        {
            var client = await CacheClientFactory.GetServiceRemotingProxy(CacheServiceUri, key).ConfigureAwait(false);
            var result = await client.StringGetAsync(GetCacheKey(key, entityName)).ConfigureAwait(false);
            return Deserialize<TEntity>(result);
        }

        public async Task<bool> InsertOrUpdateCollectionInKeyStoreAsync(string key, IEnumerable<TEntity> entityCollection, string entityName = null, TimeSpan? expiry = default(TimeSpan?))
        {
            var client = await CacheClientFactory.GetServiceRemotingProxy(CacheServiceUri, key).ConfigureAwait(false);
            return await client.StringSetAsync(GetCacheKey(key, entityName), Serialize(entityCollection), null).ConfigureAwait(false);
        }

        public async Task<bool> InsertOrUpdateInKeyStoreAsync(string key, TEntity entity, string entityName = null, TimeSpan? expiry = default(TimeSpan?))
        {
            var client = await CacheClientFactory.GetServiceRemotingProxy(CacheServiceUri, key).ConfigureAwait(false);
            return await client.StringSetAsync(GetCacheKey(key, entityName), Serialize(entity), null).ConfigureAwait(false);
        }
        #endregion

        private async Task ClearPartitionAsync(int partitionId)
        {
            var client = CacheClientFactory.GetServiceRemotingProxy(CacheServiceUri, partitionId);
            await client.ClearAllAsync().ConfigureAwait(false);
        }

        private static string GetCacheKey(string key, string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}^{1}", typeof(TEntity).FullName, key);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}^{1}", entityName, key);
        }

        /// <summary>
        ///     The serialize.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        ///     The byte array
        /// </returns>
        private static string Serialize(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            return JsonConvert.SerializeObject(
                obj, 
                Formatting.None, 
                new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
                });
        }

        /// <summary>
        ///     The deserialize.
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="stream">The stream.</param>
        /// <returns>
        ///     The <see cref="T" />.
        /// </returns>
        private static T Deserialize<T>(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(value);
        }
    }
}
