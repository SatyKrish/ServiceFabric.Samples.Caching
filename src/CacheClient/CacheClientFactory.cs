using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.FabricTransport.Common;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Client;
using Murmur;
using ServiceFabric.Samples.Caching.CacheCore;

namespace ServiceFabric.Samples.Caching.CacheClient
{
    public static class CacheClientFactory
    {
        /// <summary>
        /// The concurrent lock
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> ConcurrentLock = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// The service proxies
        /// </summary>
        private static readonly Dictionary<string, ICacheService> ServiceProxies = new Dictionary<string, ICacheService>();

        /// <summary>
        /// The service proxies
        /// </summary>
        private static readonly Dictionary<string, int> ServicePartitionCount = new Dictionary<string, int>();

        /// <summary>
        /// Gets the service remoting proxy handler.
        /// </summary>
        /// <typeparam name="T">The service.</typeparam>
        /// <param name="serviceNameUri">The service name URI.</param>
        /// <param name="partitionKey">The ESN.</param>
        /// <returns>
        /// The service remoting proxy handler.
        /// </returns>
        public static async Task<ICacheService> GetServiceRemotingProxy(string serviceNameUri, string partitionKey)
        {
            try
            {
                var partitionCount = await GetPartitionCount(serviceNameUri).ConfigureAwait(false);
                var partitionId = GetPartitionId(partitionKey, partitionCount);

                Trace.TraceInformation("Retrieving an instance of Service Remoting client for Partition - {0}.", partitionId);
                return GetServiceRemotingProxy(serviceNameUri, partitionId);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Retrieving an instance of Service Remoting client failed due to an exception. \n{0}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the service remoting proxy handler.
        /// </summary>
        /// <param name="serviceUri"></param>
        /// <param name="partitionId"></param>
        /// <returns></returns>
        public static ICacheService GetServiceRemotingProxy(string serviceUri, long? partitionId)
        {
            ICacheService serviceProxy;
            var partitionKey = partitionId.HasValue ? partitionId.ToString() : "SingletonPartition";
            var cacheKey = string.Format(typeof(ICacheService).Name + "_" + serviceUri + "_" + partitionKey);
            ServiceProxies.TryGetValue(cacheKey, out serviceProxy);
            if (serviceProxy == null)
            {
                lock (ConcurrentLock.GetOrAdd(typeof(ICacheService), new object()))
                {
                    // recheck again after entering lock, since while waiting for lock, other call might have created the proxy already
                    ServiceProxies.TryGetValue(cacheKey, out serviceProxy);

                    var fabricTransportSettings = new FabricTransportSettings() { OperationTimeout = TimeSpan.FromSeconds(60) };
                    var retrySettings = new OperationRetrySettings(TimeSpan.FromSeconds(5), TimeSpan.MaxValue, 3);
                    var clientFactory = new FabricTransportServiceRemotingClientFactory(fabricTransportSettings);
                    var serviceProxyFactory = new ServiceProxyFactory((c) => clientFactory, retrySettings);

                    if (serviceProxy == null)
                    {
                        // if not in cache, create the proxy
                        Trace.TraceInformation("Creating proxy for service {0} with URL {1}", typeof(ICacheService).Name, serviceUri);
                        if (partitionId.HasValue)
                        {
                            serviceProxy = serviceProxyFactory.CreateServiceProxy<ICacheService>(
                                new Uri(serviceUri),
                                new ServicePartitionKey(partitionId.Value));
                        }
                        else
                        {
                            serviceProxy = serviceProxyFactory.CreateServiceProxy<ICacheService>(new Uri(serviceUri));
                        }

                        ServiceProxies[cacheKey] = serviceProxy;
                    }
                }
            }

            Trace.TraceInformation("Returning proxy for service {0} with URL {1}", typeof(ICacheService).Name, serviceUri);
            return serviceProxy;
        }

        /// <summary>
        /// Gets the partition count for a given service
        /// </summary>
        /// <param name="serviceNameUri">The URI of the service.</param>
        /// <returns>The partition count.</returns>
        private static async Task<int> GetPartitionCount(string serviceNameUri)
        {
            // Get the partition count if already determined
            int partitionCount;
            ServicePartitionCount.TryGetValue(serviceNameUri, out partitionCount);
            if (partitionCount != 0)
            {
                return partitionCount;
            }

            var partitionList = await GetPartitionList(serviceNameUri).ConfigureAwait(false);
            partitionCount = partitionList?.Count ?? 0;
            ServicePartitionCount[serviceNameUri] = partitionCount;

            return partitionCount;
        }
        /// <summary>
        /// Gets the list of partitions for a given service
        /// </summary>
        /// <param name="serviceNameUri">The URI of the service.</param>
        /// <returns>The partition list.</returns>
        public static async Task<ServicePartitionList> GetPartitionList(string serviceNameUri)
        {
            var fabricClient = new FabricClient();
            var serviceDescription = await fabricClient.ServiceManager.GetServiceDescriptionAsync(new Uri(serviceNameUri)).ConfigureAwait(false);
            if (serviceDescription.PartitionSchemeDescription.Scheme != PartitionScheme.Singleton &&
                serviceDescription.PartitionSchemeDescription.Scheme != PartitionScheme.Invalid)
            {
                return await fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceNameUri)).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Gets the partition identifier.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="partitionCount">The partition count.</param>
        /// <returns>The partition identifier.</returns>
        private static long? GetPartitionId(string partitionKey, int partitionCount)
        {
            if (partitionCount == 0)
            {
                return null;
            }

            var hash = MurmurHash.Create128(0, false).ComputeHash(Encoding.ASCII.GetBytes(partitionKey));
            return (Math.Abs(BitConverter.ToInt64(hash, 0)) % partitionCount) + 1;
        }
    }
}
