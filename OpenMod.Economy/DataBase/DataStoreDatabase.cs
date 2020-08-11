#region

using System;
using System.Threading.Tasks;
using OpenMod.API.Persistence;
using OpenMod.API.Plugins;
using OpenMod.Economy.API;
using OpenMod.Economy.Classes;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class DataStoreDatabase : EconomyDatabaseCore
    {
        private readonly IEconomyDispatcher m_EconomyDispatcher;

        public DataStoreDatabase(IEconomyDispatcher dispatcher, IPluginAccessor<Economy> economyPlugin) : base(
            economyPlugin)
        {
            m_EconomyDispatcher = dispatcher;
        }

        private IDataStore DataStore => EconomyPlugin.Instance.DataStore;

        public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(async () =>
            {
                var data = await DataStore.LoadAsync<AccountsCollection>(TableName) ??
                           Activator.CreateInstance<AccountsCollection>();

                tcs.SetResult(data.Accounts.TryGetValue(uniqueId, out var balance) ? balance : DefaultBalance);
            }, exception => tcs.SetException(exception));

            return tcs.Task;
        }

        public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string _)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(async () =>
            {
                var data = await DataStore.LoadAsync<AccountsCollection>(TableName) ??
                           Activator.CreateInstance<AccountsCollection>();
                if (!data.Accounts.TryGetValue(uniqueId, out var balance)) balance = DefaultBalance;

                var newBalance = balance + amount;
                if (newBalance < 0)
                    throw new NotEnoughBalanceException(
                        StringLocalizer["economy:fail:not_enough_balance", new {Balance = balance, CurrencySymbol}],
                        balance);

                data.Accounts[uniqueId] = newBalance;
                await DataStore.SaveAsync(TableName, data);

                tcs.SetResult(newBalance);
            }, exception => tcs.SetException(exception));

            return tcs.Task;
        }

        public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(async () =>
            {
                var data = await DataStore.LoadAsync<AccountsCollection>(TableName) ??
                           Activator.CreateInstance<AccountsCollection>();

                data.Accounts[uniqueId] = balance;
                await DataStore.SaveAsync(TableName, data);
            }, exception => tcs.SetException(exception));

            return tcs.Task;
        }
    }
}