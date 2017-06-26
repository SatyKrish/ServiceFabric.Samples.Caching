using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.Samples.Caching.CacheCore;

namespace ServiceFabric.Samples.Caching.CacheService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class CacheService : StatefulService, ICacheService
    {
        private IReliableDictionary<string, string> cacheDictionary;

        public CacheService(StatefulServiceContext context)
            : base(context)
        { }

        #region Stateless Service - Override
        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(this.CreateServiceRemotingListener)
            };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            cacheDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("cacheDictionary").ConfigureAwait(false);
            ServiceEventSource.Current.ServiceMessage(this.Context, "CacheService run async completed.");
        }
        #endregion

        #region ICacheService - Implementation
        public async Task ClearAllAsync()
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "ClearAllAsync operation called.");
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    await cacheDictionary.ClearAsync().ConfigureAwait(false);
                    await tx.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "ClearAllAsync operation failed due to an unhandled exception - {0}.", ex);
            }
        }

        public async Task<bool> KeyDeleteAsync(string key)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "KeyDeleteAsync operation called for key - {0}.", key);
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await cacheDictionary.TryRemoveAsync(tx, key).ConfigureAwait(false);
                    await tx.CommitAsync();
                    return result.HasValue ? true : false;
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "KeyDeleteAsync operation failed due to an unhandled exception - {0}.", ex);
                return false;
            }
        }

        public async Task<bool> KeysDeleteAsync(string[] keys)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "KeyDeleteAsync operation called for keys - {0}.", String.Join(",", keys));
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var deleteTasks = keys.Select(key => cacheDictionary.TryRemoveAsync(tx, key));
                    await Task.WhenAll(deleteTasks).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "KeyDeleteAsync operation failed due to an unhandled exception - {0}.", ex);
                return false;
            }
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "KeyExistsAsync operation called for key - {0}.", key);
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    return await cacheDictionary.ContainsKeyAsync(tx, key).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "KeyExistsAsync operation failed due to an unhandled exception - {0}.", ex);
                return false;
            }
        }

        public async Task<string> StringGetAsync(string key)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "StringGetAsync operation called for key - {0}.", key);
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await this.cacheDictionary.TryGetValueAsync(tx, key);
                    return result.HasValue ? result.Value : string.Empty;
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "StringGetAsync operation failed due to an unhandled exception - {0}.", ex);
                return string.Empty;
            }
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "StringSetAsync operation called for key - {0}.", key);
            ServiceEventSource.Current.ServiceMessage(this.Context, @"{0}:\n{1}", key, value);
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    if (await cacheDictionary.ContainsKeyAsync(tx, key).ConfigureAwait(false))
                    {
                        await cacheDictionary.SetAsync(tx, key, value).ConfigureAwait(false);
                    }
                    else
                    {
                        await cacheDictionary.AddAsync(tx, key, value).ConfigureAwait(false);
                    }

                    await tx.CommitAsync().ConfigureAwait(false);
                }

                return true;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "StringSetAsync operation failed due to an unhandled exception - {0}.", ex);
                return false;
            }
        }
        #endregion
    }
}
