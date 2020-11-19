#region

using System.Threading.Tasks;
using OpenMod.API.Plugins;
using OpenMod.API.Users;
using OpenMod.Economy.API;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Database
{
    internal sealed class UserDataDatabase : EconomyDatabaseCore
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
                tcs.SetResult(await m_UserDataStore.GetUserDataAsync<decimal?>(ownerId, ownerType, TableName) ??
                              DefaultBalance);
            });

            return tcs.Task;
        }

        public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string _)
        {
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(async () =>
            {
                var balance = await m_UserDataStore.GetUserDataAsync<decimal?>(ownerId, ownerType, TableName) ??
                              DefaultBalance;

                var newBalance = balance + amount;
                if (newBalance < 0)
                    throw new NotEnoughBalanceException(
                        StringLocalizer["economy:fail:not_enough_balance",
                            new {Balance = balance - amount, CurrencySymbol}], balance);

                await m_UserDataStore.SetUserDataAsync(ownerId, ownerType, TableName, balance);

                tcs.SetResult(balance);
            });

            return tcs.Task;
        }

        public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var tcs = new TaskCompletionSource<decimal>();
            m_EconomyDispatcher.Enqueue(() => m_UserDataStore.SetUserDataAsync(ownerId, ownerType, TableName, balance));
            return tcs.Task;
        }
    }
}