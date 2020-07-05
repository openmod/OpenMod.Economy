#region

using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Database.Helper;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class UserDataDatabase : UserDataHelper, IEconomyInternalDatabase
    {
        private readonly decimal m_DefaultBalance;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly string m_TableName;

        public UserDataDatabase(decimal defaultBalance,
            IStringLocalizer stringLocalizer, string tableName, IUserDataStore userDataStore) : base(userDataStore)
        {
            m_DefaultBalance = defaultBalance;
            m_StringLocalizer = stringLocalizer;
            m_TableName = tableName;
        }

        public Task<bool> CreateUserAccountAsync(string userId, string userType)
        {
            return ExecuteUserDataContextAsync(userId, userType, data =>
            {
                if (data.ContainsKey(m_TableName)) return false;

                data.Add(m_TableName, m_DefaultBalance);
                return true;
            });
        }

        public Task<decimal> GetBalanceAsync(string userId, string userType)
        {
            return ExecuteUserDataContextAsync(userId, userType, data =>
            {
                if (data.TryGetValue(m_TableName, out var balance)) 
                    return (decimal) balance;

                data.Add(m_TableName, m_DefaultBalance);
                return m_DefaultBalance;
            });
        }

        public Task<decimal> IncreaseBalanceAsync(string userId, string userType, decimal amount)
        {
            return ExecuteUserDataContextAsync(userId, userType, data =>
            {
                decimal balance;
                if (data.TryGetValue(m_TableName, out var currentBalance))
                {
                    balance = (decimal) currentBalance + amount;
                    data[m_TableName] = balance;
                    return balance;
                }

                balance = m_DefaultBalance + amount;
                data.Add(m_TableName, balance);
                return m_DefaultBalance;
            });
        }

        public Task<decimal> DecreaseBalanceAsync(string userId, string userType, decimal amount,
            bool allowNegativeBalance)
        {
            return ExecuteUserDataContextAsync(userId, userType, data =>
            {
                decimal currentBalance;
                if (data.TryGetValue(m_TableName, out var currentBalanceObj))
                    currentBalance = (decimal) currentBalanceObj;
                else
                    currentBalance = m_DefaultBalance;

                var balance = currentBalance + amount;
                if (!allowNegativeBalance && balance < 0)
                    throw new UserFriendlyException(m_StringLocalizer["uconomy:fail:not_enough_balance"]);

                data[m_TableName] = balance;
                return balance;
            });
        }
    }
}