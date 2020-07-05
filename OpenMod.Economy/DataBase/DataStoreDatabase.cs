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

        public Task<decimal> GetBalanceAsync(IAccountId accountId)
        {
            return ExecuteDataStoreContextAsync<AccountsCollection, decimal>(accountsData =>
            {
                if (accountsData.Accounts.TryGetValue(accountId, out var balance))
                    return balance;

                accountsData.Accounts.Add(accountId, m_DefaultBalance);
                return m_DefaultBalance;
            });
        }

        public Task<decimal> UpdateBalanceAsync(IAccountId accountId, decimal amount)
        {
            return ExecuteDataStoreContextAsync<AccountsCollection, decimal>(accountsData =>
            {
                if (!accountsData.Accounts.TryGetValue(accountId, out var balance))
                    balance = m_DefaultBalance;

                balance += amount;
                if (balance < 0)
                    throw new UserFriendlyException(m_StringLocalizer["economy:fail:not_enough_balance", balance]);

                return accountsData.Accounts[accountId] = balance;
            });
        }

        public Task SetAccountAsync(IAccountId accountId, decimal balance)
        {
            return ExecuteDataStoreContextAsync<AccountsCollection>(accountsData =>
            {
                accountsData.Accounts[accountId] = balance;
            });
        }
    }
}