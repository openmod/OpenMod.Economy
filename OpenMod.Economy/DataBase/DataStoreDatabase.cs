#region

using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using OpenMod.API.Persistence;
using OpenMod.API.Plugins;
using OpenMod.Economy.Core;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class DataStoreDatabase : DataBaseCore
    {
        public DataStoreDatabase(IPluginAccessor<Economy> economyPlugin) : base(economyPlugin)
        {
        }

        private IDataStore m_DataStore => EconomyPlugin.Instance.DataStore;

        public override async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            try
            {
                await UniTask.SwitchToMainThread();
                var data = await m_DataStore.LoadAsync<AccountsCollection>(TableName) ??
                           Activator.CreateInstance<AccountsCollection>();
                return data.Accounts.TryGetValue(uniqueId, out var balance) ? balance : DefaultBalance;
            }
            finally
            {
                await UniTask.Yield();
            }
        }

        public override async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            try
            {
                await UniTask.SwitchToMainThread();
                var data = await m_DataStore.LoadAsync<AccountsCollection>(TableName) ??
                           Activator.CreateInstance<AccountsCollection>();
                if (!data.Accounts.TryGetValue(uniqueId, out var balance)) balance = DefaultBalance;

                balance += amount;
                if (balance < 0)
                    throw new NotEnoughBalanceException(StringLocalizer["economy:fail:not_enough_balance", balance]);

                return data.Accounts[uniqueId] = balance;
            }
            finally
            {
                await UniTask.Yield();
            }
        }

        public override async Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            try
            {
                await UniTask.SwitchToMainThread();
                var data = await m_DataStore.LoadAsync<AccountsCollection>(TableName) ??
                           Activator.CreateInstance<AccountsCollection>();
                data.Accounts[uniqueId] = balance;
            }
            finally
            {
                await UniTask.Yield();
            }
        }
    }
}