#region

using System;
using System.Threading.Tasks;
using OpenMod.API.Persistence;

#endregion

namespace OpenMod.Economy.Helpers
{
    public abstract class DataStoreHelper : ThreadHelper
    {
        private readonly string m_DataKey;
        private readonly IDataStore m_DataStore;

        protected DataStoreHelper(string dataKey, IDataStore dataStore)
        {
            m_DataKey = dataKey;
            m_DataStore = dataStore;
        }

        public Task ExecuteDataStoreContextAsync<T>(Action<T> action, bool save = true) where T : class, new()
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                var data = await m_DataStore.LoadAsync<T>(m_DataKey) ?? Activator.CreateInstance<T>();
                action.Invoke(data);

                if (save)
                    await m_DataStore.SaveAsync(m_DataKey, data);
            });
        }

        public Task<TResult> ExecuteDataStoreContextAsync<T, TResult>(Func<T, TResult> action, bool save = true) where T : class, new()
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                var data = await m_DataStore.LoadAsync<T>(m_DataKey) ?? Activator.CreateInstance<T>();
                var result = action.Invoke(data);

                if (save)
                    await m_DataStore.SaveAsync(m_DataKey, data);
                return result;
            });
        }
    }
}