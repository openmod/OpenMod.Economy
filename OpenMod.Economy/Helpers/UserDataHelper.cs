#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMod.API.Users;

#endregion

namespace OpenMod.Economy.Helpers
{
    public abstract class UserDataHelper : ThreadHelper
    {
        private readonly IUserDataStore m_UserDataStore;

        protected UserDataHelper(IUserDataStore userDataStore)
        {
            m_UserDataStore = userDataStore;
        }

        public Task ExecuteUserDataContextAsync(string ownerId, string ownerType, Action<Dictionary<string, object>> action, bool save = true)
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                var userData = await m_UserDataStore.GetUserDataAsync(ownerId, ownerType);
                userData.Data ??= new Dictionary<string, object>();

                action.Invoke(userData.Data);
                if (save)
                    await m_UserDataStore.SaveUserDataAsync(userData);
            });
        }

        public Task<T> ExecuteUserDataContextAsync<T>(string ownerId, string ownerType,
            Func<Dictionary<string, object>, T> action, bool save = true)
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                var userData = await m_UserDataStore.GetUserDataAsync(ownerId, ownerType);
                userData.Data ??= new Dictionary<string, object>();

                var result = action.Invoke(userData.Data);
                if (save) 
                    await m_UserDataStore.SaveUserDataAsync(userData);
                return result;
            });
        }
    }
}