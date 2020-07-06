#region

using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Users;
using OpenMod.Economy.API;
using OpenMod.Economy.Helpers;
using OpenMod.Extensions.Economy.Abstractions;

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

        public Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            return ExecuteUserDataContextAsync(ownerId, ownerType, data =>
            {
                if (data.TryGetValue(m_TableName, out var balance))
                    return (decimal) balance;

                data.Add(m_TableName, m_DefaultBalance);
                return m_DefaultBalance;
            });
        }

        public Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            return ExecuteUserDataContextAsync(ownerId, ownerType, data =>
            {
                decimal balance;
                if (data.TryGetValue(m_TableName, out var balanceObj))
                    balance = (decimal) balanceObj;
                else
                    balance = m_DefaultBalance;

                balance += amount;
                if (balance < 0)
                    throw new NotEnoughBalanceException(m_StringLocalizer["economy:fail:not_enough_balance"]);

                data[m_TableName] = balance;
                return balance;
            });
        }

        public Task SetAccountAsync(string ownerId, string ownerType, decimal balance)
        {
            return ExecuteUserDataContextAsync(ownerId, ownerType,
                data => { data[m_TableName] = balance; });
        }
    }
}