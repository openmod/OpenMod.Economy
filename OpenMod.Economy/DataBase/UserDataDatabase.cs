#region

using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMod.API.Plugins;
using OpenMod.API.Users;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class UserDataDatabase : DataBaseCore
    {
        private readonly IUserDataStore m_UserDataStore;

        public UserDataDatabase(IPluginAccessor<Economy> economyPlugin, IUserDataStore userDataStore) : base(
            economyPlugin)
        {
            m_UserDataStore = userDataStore;
        }

        public override async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var userData = await m_UserDataStore.GetUserDataAsync(ownerId, ownerType);
            userData.Data ??= new Dictionary<string, object>();

            if (userData.Data.TryGetValue(TableName, out var balance)) return (decimal) balance;

            return DefaultBalance;
        }

        public override async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
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
                throw new NotEnoughBalanceException(StringLocalizer["economy:fail:not_enough_balance"]);

            userData.Data[TableName] = balance;
            return balance;
        }

        public override async Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var userData = await m_UserDataStore.GetUserDataAsync(ownerId, ownerType);
            userData.Data ??= new Dictionary<string, object>();

            userData.Data[TableName] = balance;
        }
    }
}