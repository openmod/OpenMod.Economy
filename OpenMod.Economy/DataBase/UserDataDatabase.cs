#region

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class UserDataDatabase : IEconomyInternalDatabase
    {
        private readonly decimal m_DefaultBalance;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly string m_TableName;
        private readonly IUserDataStore m_UserDataStore;

        internal UserDataDatabase(IConfiguration configuration, decimal defaultBalance,
            IStringLocalizer stringLocalizer, IUserDataStore userDataStore)
        {
            m_DefaultBalance = defaultBalance;
            m_StringLocalizer = stringLocalizer;
            m_TableName = configuration["Table_Name"];
            m_UserDataStore = userDataStore;
        }

        public async Task<bool> CreateUserAccountAsync(string userId, string userType)
        {
            var userData = await m_UserDataStore.GetUserDataAsync(userId, userType);
            if (userData.Data.ContainsKey(m_TableName))
                return false;

            userData.Data.Add(m_TableName, m_DefaultBalance);
            await m_UserDataStore.SaveUserDataAsync(userData);
            return true;
        }

        public async Task<decimal> GetBalanceAsync(string userId, string userType)
        {
            var userData = await m_UserDataStore.GetUserDataAsync(userId, userType);
            if (userData.Data.TryGetValue(m_TableName, out var balance))
                return (decimal) balance;

            userData.Data.Add(m_TableName, m_DefaultBalance);
            await m_UserDataStore.SaveUserDataAsync(userData);
            return m_DefaultBalance;
        }

        public async Task<decimal> IncreaseBalanceAsync(string userId, string userType, decimal amount)
        {
            decimal balance;
            var userData = await m_UserDataStore.GetUserDataAsync(userId, userType);
            if (userData.Data.TryGetValue(m_TableName, out var currentBalance))
            {
                balance = (decimal) currentBalance + amount;
                userData.Data[m_TableName] = balance;
                await m_UserDataStore.SaveUserDataAsync(userData);
                return balance;
            }

            balance = m_DefaultBalance + amount;
            userData.Data.Add(m_TableName, balance);
            await m_UserDataStore.SaveUserDataAsync(userData);
            return m_DefaultBalance;
        }

        public async Task<decimal> DecreaseBalanceAsync(string userId, string userType, decimal amount,
            bool allowNegativeBalance)
        {
            decimal currentBalance;
            var userData = await m_UserDataStore.GetUserDataAsync(userId, userType);
            if (userData.Data.TryGetValue(m_TableName, out var currentBalanceObj))
                currentBalance = (decimal) currentBalanceObj;
            else
                currentBalance = m_DefaultBalance;

            var balance = currentBalance + amount;
            if (!allowNegativeBalance && balance < 0)
                throw new UserFriendlyException(m_StringLocalizer["uconomy:fail:not_enough_balance"]);

            userData.Data[m_TableName] = balance;
            await m_UserDataStore.SaveUserDataAsync(userData);
            return balance;
        }
    }
}