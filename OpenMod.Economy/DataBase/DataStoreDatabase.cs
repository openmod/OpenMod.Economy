#region

using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Persistence;
using OpenMod.Core.Commands;
using OpenMod.Database.Helper;
using OpenMod.Economy.API;
using OpenMod.Economy.Core;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class DataStoreDatabase : DataStoreHelper, IEconomyInternalDatabase
    {
        private readonly decimal m_DefaultBalance;
        private readonly IStringLocalizer m_StringLocalizer;

        public DataStoreDatabase(IDataStore dataStore, decimal defaultBalance, IStringLocalizer stringLocalizer,
            string tableName) : base(tableName, dataStore)
        {
            m_DefaultBalance = defaultBalance;
            m_StringLocalizer = stringLocalizer;
        }

        public Task<bool> CreateUserAccountAsync(string userId, string userType)
        {
            var uniqueId = $"{userType}_{userId}";
            return ExecuteDataStoreContextAsync<UserAccounts, bool>(accounts =>
            {
                if (accounts.Accounts.ContainsKey(uniqueId))
                    return false;

                accounts.Accounts.Add(uniqueId, m_DefaultBalance);
                return true;
            });
        }

        public Task<decimal> GetBalanceAsync(string userId, string userType)
        {
            var uniqueId = $"{userType}_{userId}";
            return ExecuteDataStoreContextAsync<UserAccounts, decimal>(accounts =>
            {
                if (accounts.Accounts.TryGetValue(uniqueId, out var balance))
                    return balance;

                accounts.Accounts.Add(uniqueId, m_DefaultBalance);
                return m_DefaultBalance;
            });
        }

        public Task<decimal> IncreaseBalanceAsync(string userId, string userType, decimal amount)
        {
            var uniqueId = $"{userType}_{userId}";
            return ExecuteDataStoreContextAsync<UserAccounts, decimal>(accounts =>
            {
                if (accounts.Accounts.ContainsKey(uniqueId)) return accounts.Accounts[uniqueId] += amount;

                var balance = m_DefaultBalance + amount;
                accounts.Accounts.Add(uniqueId, balance);
                return balance;
            });
        }

        public Task<decimal> DecreaseBalanceAsync(string userId, string userType, decimal amount,
            bool allowNegativeBalance)
        {
            var uniqueId = $"{userType}_{userId}";
            return ExecuteDataStoreContextAsync<UserAccounts, decimal>(accounts =>
            {
                if (!accounts.Accounts.TryGetValue(uniqueId, out var currentBalance)) currentBalance = m_DefaultBalance;

                var balance = currentBalance - amount;
                if (!allowNegativeBalance && balance < 0)
                    throw new UserFriendlyException(m_StringLocalizer["uconomy:fail:not_enough_balance", balance]);
                return accounts.Accounts[uniqueId] = balance;
            });
        }
    }
}