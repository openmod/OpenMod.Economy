#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Persistence;
using OpenMod.Economy.API;
using OpenMod.Economy.Classes;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class DataStoreDatabase : EconomyDatabaseCore
    {
        private readonly IDataStore m_DataStore;
        private readonly IEconomyDispatcher m_Dispatcher;
        private readonly IStringLocalizer m_StringLocalizer;

        public DataStoreDatabase(IConfiguration configuration, IDataStore dataStore, IEconomyDispatcher dispatcher,
            IStringLocalizer stringLocalizer) : base(configuration)
        {
            m_DataStore = dataStore;
            m_Dispatcher = dispatcher;
            m_StringLocalizer = stringLocalizer;
        }

        public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return m_Dispatcher.EnqueueV2(async () =>
            {
                var data = (AccountsCollection) null;
                if (await m_DataStore.ExistsAsync(TableName))
                    data = await m_DataStore.LoadAsync<AccountsCollection>(TableName);

                data ??= Activator.CreateInstance<AccountsCollection>();
                return data.Accounts.TryGetValue(uniqueId, out var balance) ? balance : DefaultBalance;
            });
        }

        public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string _)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return m_Dispatcher.EnqueueV2(async () =>
            {
                var data = (AccountsCollection) null;
                if (await m_DataStore.ExistsAsync(TableName))
                    data = await m_DataStore.LoadAsync<AccountsCollection>(TableName);

                data ??= Activator.CreateInstance<AccountsCollection>();
                if (!data.Accounts.TryGetValue(uniqueId, out var balance)) balance = DefaultBalance;

                var newBalance = balance + amount;
                if (newBalance < 0)
                    throw new NotEnoughBalanceException(
                        m_StringLocalizer["economy:fail:not_enough_balance",
                            new {Amount = -amount, Balance = balance, EconomyProvider = (IEconomyProvider) this}],
                        balance);

                data.Accounts[uniqueId] = newBalance;
                await m_DataStore.SaveAsync(TableName, data);
                return newBalance;
            });
        }

        public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return m_Dispatcher.EnqueueV2(async () =>
            {
                var data = (AccountsCollection) null;
                if (await m_DataStore.ExistsAsync(TableName))
                    data = await m_DataStore.LoadAsync<AccountsCollection>(TableName);

                data ??= Activator.CreateInstance<AccountsCollection>();
                data.Accounts[uniqueId] = balance;
                await m_DataStore.SaveAsync(TableName, data);
            });
        }
    }
}