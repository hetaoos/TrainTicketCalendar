using Google.Apis.Util.Store;
using LiteDB;
using System.Threading.Tasks;
using TrainTicketCalendar.Data;

namespace TrainTicketCalendar.Google
{
    /// <summary>
    /// Stores and manages data objects, where the key is a string and the value is an object.
    /// <para>
    /// <c>null</c> keys are not allowed.
    /// </para>
    /// </summary>
    public class DbDataStore : IDataStore
    {
        private readonly ApplicationDbContext db;
        private readonly LiteCollection<GoogleDataStore> col;

        public DbDataStore(ApplicationDbContext db)
        {
            this.db = db;
            col = db.GetCollection<GoogleDataStore>();
        }

        /// <summary>Asynchronously clears all values in the data store.</summary>
        public Task ClearAsync()
        {
            col.Delete(o => true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously deletes the given key. The type is provided here as well because the "real" saved key should
        /// contain type information as well, so the data store will be able to store the same key for different types.
        /// </summary>
        /// <typeparam name="T">The type to delete from the data store.</typeparam>
        /// <param name="key">The key to delete.</param>
        public Task DeleteAsync<T>(string key)
        {
            col.Delete(o => o.id == key);
            return Task.CompletedTask;
        }

        /// <summary>Asynchronously returns the stored value for the given key or <c>null</c> if not found.</summary>
        /// <typeparam name="T">The type to retrieve from the data store.</typeparam>
        /// <param name="key">The key to retrieve its value.</param>
        /// <returns>The stored object.</returns>
        public Task<T> GetAsync<T>(string key)
        {
            var item = col.FindOne(o => o.id == key);
            var value = item == null ? default(T) : item.GetValue<T>();
            return Task.FromResult(value);
        }

        /// <summary>Asynchronously stores the given value for the given key (replacing any existing value).</summary>
        /// <typeparam name="T">The type to store in the data store.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to store.</param>
        public Task StoreAsync<T>(string key, T value)
        {
            var item = new GoogleDataStore() { id = key };
            item.SetValue(value);
            col.Upsert(key, item);
            return Task.CompletedTask;
        }
    }
}