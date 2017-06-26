// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICacheStrategy.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ServiceFabric.Samples.Caching.CacheClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    ///     Cache strategy interface
    /// </summary>
    /// <typeparam name="T">Type of class</typeparam>
    public interface ICacheStrategy<T>
    {
        /// <summary>
        /// Gets a value indicating whether is cache disabled.
        /// </summary>
        bool IsCacheDisabled { get; }

        #region Async Methods

        /// <summary>
        /// Checks if Key exists
        /// </summary>
        /// <param name="key">
        /// The key
        /// </param>
        /// <param name="entityName">
        /// The entity Name.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>
        /// </returns>
        Task<bool> DoesExistInKeyStoreAsync(string key, string entityName = null);

        /// <summary>
        /// The get async.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="entityName">Name of the entity.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        Task<T> GetFromKeyStoreAsync(string key, string entityName = null);

        /// <summary>
        /// Inserts the or update list in key store asynchronous.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="entityCollection">The entity.</param>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="expiry">The expiry.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        Task<bool> InsertOrUpdateCollectionInKeyStoreAsync(string key, IEnumerable<T> entityCollection, string entityName = null, TimeSpan? expiry = null);

        /// <summary>
        /// Gets the list from key store asynchronous.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="entityName">Name of the entity.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        Task<IEnumerable<T>> GetCollectionFromKeyStoreAsync(string key, string entityName = null);

        /// <summary>
        /// The insert or replace async.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="expiry">The expiry time.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        Task<bool> InsertOrUpdateInKeyStoreAsync(string key, T entity, string entityName = null, TimeSpan? expiry = null);

        /// <summary>
        /// The delete async.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="entityName">
        /// The entity Name.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<bool> DeleteFromKeyStoreAsync(string key, string entityName = null);

        /// <summary>
        /// Deletes from key store asynchronous.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="entityName">Name of the entity.</param>
        /// <returns>Count of keys deleted</returns>
        Task<long> DeleteFromKeyStoreAsync(List<string> keys, string entityName = null);

        /// <summary>
        /// The clear async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<bool> ClearAsync();

#endregion
    }
}