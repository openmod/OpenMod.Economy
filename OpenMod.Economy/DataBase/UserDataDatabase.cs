#region

using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMod.API.Plugins;
using OpenMod.API.Users;
using OpenMod.Economy.API;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class UserDataDatabase : DataBaseCore
    {
        private readonly IEconomyDispatcher m_EconomyDispatcher;
        private readonly IUserDataStore m_UserDataStore;

        public UserDataDatabase(IEconomyDispatcher dispatcher, IPluginAccessor<Economy> economyPlugin,
            IUserDataStore userDataStore) : base(
            economyPlugin)
        {
            m_EconomyDispatcher = dispatcher;
            m_UserDataStore = userDataStore;
        }

        public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(async () =>
            {
                var userData = await m_UserDataStore.GetUserDataAsync(ownerId, ownerType);
                userData.Data ??= new Dictionary<string, object>();

                var balance = DefaultBalance;
                if (userData.Data.TryGetValue(TableName, out var balanceObj))
                    balance = (decimal) balanceObj;

                tcs.SetResult(balance);
            });

            return tcs.Task;
        }

        public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(async () =>
            {
                var userData = await m_UserDataStore.GetUserDataAsync(ownerId, ownerType);
                userData.Data ??= new Dictionary<string, object>();

                decimal balance;
                if (userData.Data.TryGetValue(TableName, out var balanceObj))
                    balance = (decimal) balanceObj;
                else
                    balance = DefaultBalance;

                balance += amount;
                if (balance < 0)
                    throw new NotEnoughBalanceException(StringLocalizer["economy:fail:not_enough_balance", new { Balance = balance - amount, CurrencySymbol }]);

                userData.Data[TableName] = balance;
                await m_UserDataStore.SaveUserDataAsync(userData);

                tcs.SetResult(balance);
            });

            return tcs.Task;
        }

        public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(async () =>
            {
                var userData = await m_UserDataStore.GetUserDataAsync(ownerId, ownerType);
                userData.Data ??= new Dictionary<string, object>();

                userData.Data[TableName] = balance;
                await m_UserDataStore.SaveUserDataAsync(userData);
            });

            return tcs.Task;
        }
    }
}