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

        public Task<decimal> GetBalanceAsync(IAccountId accountId)
        {
            return ExecuteUserDataContextAsync(accountId.OwnerType, accountId.OwnerId, data =>
            {
                if (data.TryGetValue(m_TableName, out var balance))
                    return (decimal)balance;

                data.Add(m_TableName, m_DefaultBalance);
                return m_DefaultBalance;
            });
        }

        public Task<decimal> UpdateBalanceAsync(IAccountId accountId, decimal amount)
        {
            return ExecuteUserDataContextAsync(accountId.OwnerType, accountId.OwnerId, data =>
            {
                decimal balance;
                if (data.TryGetValue(m_TableName, out var balanceObj))
                    balance = (decimal)balanceObj;
                else
                    balance = m_DefaultBalance;

                balance += amount;
                if (balance < 0)
                    throw new UserFriendlyException(m_StringLocalizer["economy:fail:not_enough_balance"]);

                data[m_TableName] = balance;
                return balance;
            });
        }

        public Task SetAccountAsync(IAccountId accountId, decimal balance)
        {
            return ExecuteUserDataContextAsync(accountId.OwnerType, accountId.OwnerId, data =>
            {
                data[m_TableName] = balance;
            });
        }
    }
}