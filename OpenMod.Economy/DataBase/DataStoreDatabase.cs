#region

using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Persistence;
using OpenMod.Economy.API;
using OpenMod.Economy.Core;
using OpenMod.Economy.Helpers;
using OpenMod.Extensions.Economy.Abstractions;

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

        public Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return ExecuteDataStoreContextAsync<AccountsCollection, decimal>(accountsData =>
            {
                if (accountsData.Accounts.TryGetValue(uniqueId, out var balance))
                    return balance;

                accountsData.Accounts.Add(uniqueId, m_DefaultBalance);
                return m_DefaultBalance;
            });
        }

        public Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return ExecuteDataStoreContextAsync<AccountsCollection, decimal>(accountsData =>
            {
                if (!accountsData.Accounts.TryGetValue(uniqueId, out var balance))
                    balance = m_DefaultBalance;

                balance += amount;
                if (balance < 0)
                    throw new NotEnoughBalanceException(m_StringLocalizer["economy:fail:not_enough_balance", balance]);

                return accountsData.Accounts[uniqueId] = balance;
            });
        }

        public Task SetAccountAsync(string ownerId, string ownerType, decimal balance)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return ExecuteDataStoreContextAsync<AccountsCollection>(accountsData =>
            {
                accountsData.Accounts[uniqueId] = balance;
            });
        }
    }
}