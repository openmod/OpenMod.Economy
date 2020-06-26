#region

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Persistence;
using OpenMod.Core.Commands;
using OpenMod.Economy.API;
using OpenMod.Economy.Core;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class DataStoreDatabase : IEconomyInternalDatabase
    {
        private readonly IDataStore m_DataStore;
        private readonly decimal m_DefaultBalance;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly string m_TableName;

        internal DataStoreDatabase(IConfiguration configuration, IDataStore dataStore, decimal defaultBalance,
            IStringLocalizer stringLocalizer)
        {
            m_DataStore = dataStore;
            m_DefaultBalance = defaultBalance;
            m_StringLocalizer = stringLocalizer;
            m_TableName = configuration["Table_Name"];
        }

        public async Task<bool> CreateUserAccountAsync(string userId, string userType)
        {
            var uniqueId = $"{userType}_{userId}";
            var accounts = await m_DataStore.LoadAsync<UserAccounts>(m_TableName) ?? new UserAccounts();
            if (accounts.Accounts.ContainsKey(uniqueId))
                return false;

            accounts.Accounts.Add(uniqueId, m_DefaultBalance);
            await m_DataStore.SaveAsync(m_TableName, accounts);
            return true;
        }

        public async Task<decimal> GetBalanceAsync(string userId, string userType)
        {
            var uniqueId = $"{userType}_{userId}";
            var accounts = await m_DataStore.LoadAsync<UserAccounts>(m_TableName) ?? new UserAccounts();
            if (accounts.Accounts.TryGetValue(uniqueId, out var balance))
                return balance;

            accounts.Accounts.Add(uniqueId, m_DefaultBalance);
            await m_DataStore.SaveAsync(m_TableName, accounts);
            return m_DefaultBalance;
        }

        public async Task<decimal> IncreaseBalanceAsync(string userId, string userType, decimal amount)
        {
            decimal balance;
            var uniqueId = $"{userType}_{userId}";
            var accounts = await m_DataStore.LoadAsync<UserAccounts>(m_TableName) ?? new UserAccounts();
            if (accounts.Accounts.ContainsKey(uniqueId))
            {
                balance = accounts.Accounts[uniqueId] += amount;
                await m_DataStore.SaveAsync(m_TableName, accounts);
                return balance;
            }

            balance = m_DefaultBalance + amount;
            accounts.Accounts.Add(uniqueId, balance);
            await m_DataStore.SaveAsync(m_TableName, accounts);
            return balance;
        }

        public async Task<decimal> DecreaseBalanceAsync(string userId, string userType, decimal amount,
            bool allowNegativeBalance)
        {
            var uniqueId = $"{userType}_{userId}";
            var accounts = await m_DataStore.LoadAsync<UserAccounts>(m_TableName) ?? new UserAccounts();
            if (!accounts.Accounts.TryGetValue(uniqueId, out var currentBalance))
                currentBalance = m_DefaultBalance;

            var balance = currentBalance - amount;
            if (!allowNegativeBalance && balance < 0)
                throw new UserFriendlyException(m_StringLocalizer["uconomy:fail:not_enough_balance", balance]);

            accounts.Accounts[uniqueId] = balance;
            await m_DataStore.SaveAsync(m_TableName, accounts);
            return balance;
        }
    }
}