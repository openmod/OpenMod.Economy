#region

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Users;
using OpenMod.Economy.API;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class UserDataDatabase : EconomyDatabaseCore
    {
        private readonly IEconomyDispatcher m_Dispatcher;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IUserDataStore m_UserDataStore;

        public UserDataDatabase(IConfiguration configuration, IEconomyDispatcher dispatcher,
            IStringLocalizer stringLocalizer, IUserDataStore userDataStore) : base(configuration)
        {
            m_Dispatcher = dispatcher;
            m_StringLocalizer = stringLocalizer;
            m_UserDataStore = userDataStore;
        }

        public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            return m_Dispatcher.EnqueueV2(async () =>
                await m_UserDataStore.GetUserDataAsync<decimal?>(ownerId, ownerType, TableName) ?? DefaultBalance);
        }

        public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string _)
        {
            return m_Dispatcher.EnqueueV2(async () =>
            {
                var balance = await m_UserDataStore.GetUserDataAsync<decimal?>(ownerId, ownerType, TableName) ??
                              DefaultBalance;

                var newBalance = balance + amount;
                if (newBalance < 0)
                    throw new NotEnoughBalanceException(
                        m_StringLocalizer["economy:fail:not_enough_balance",
                            new {Amount = amount, Balance = balance, EconomyProvider = (IEconomyProvider) this}],
                        balance);

                await m_UserDataStore.SetUserDataAsync(ownerId, ownerType, TableName, balance);
                return balance;
            });
        }

        public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            return m_Dispatcher.EnqueueV2(
                () => m_UserDataStore.SetUserDataAsync(ownerId, ownerType, TableName, balance));
        }
    }
}