#region

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.Core.Commands;
using OpenMod.Database.Helper;
using OpenMod.Economy.API;
using OpenMod.Economy.Core;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class LiteDatabase : LiteDbHelper, IEconomyInternalDatabase
    {
        private readonly decimal m_DefaultBalance;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly string m_TableName;

        public LiteDatabase(IConfiguration configuration, decimal defaultBalance, IStringLocalizer stringLocalizer,
            string tableName) : base(configuration["LiteDb_Connection_String"])
        {
            m_DefaultBalance = defaultBalance;
            m_StringLocalizer = stringLocalizer;
            m_TableName = tableName;
        }

        public Task<decimal> GetBalanceAsync(IAccountId accountId)
        {
            return ExecuteLiteDbContextAsync(db =>
            {
                var accounts = db.GetCollection<AccountBase>(m_TableName);
                var account = accounts.FindById(accountId.UniqueId);
                return account.Balance;
            });
        }

        public Task<decimal> UpdateBalanceAsync(IAccountId accountId, decimal amount)
        {
            return ExecuteLiteDbContextAsync(db =>
            {
                var accounts = db.GetCollection<AccountBase>(m_TableName);
                var account = accounts.FindById(accountId.UniqueId) ?? new AccountBase
                {
                    UniqueId = accountId.UniqueId,
                    Balance = m_DefaultBalance
                };

                account.Balance += amount;
                if (account.Balance < 0)
                    throw new UserFriendlyException(m_StringLocalizer["economy:fail:not_enough_balance", account.Balance]);

                accounts.Upsert(account);
                return account.Balance;
            });
        }

        public Task SetAccountAsync(IAccountId accountId, decimal balance)
        {
            return ExecuteLiteDbContextAsync(db =>
            {
                var accounts = db.GetCollection<AccountBase>(m_TableName);
                var account = accounts.FindById(accountId.UniqueId) ?? new AccountBase
                {
                    UniqueId = accountId.UniqueId,
                };

                account.Balance = balance;
                accounts.Upsert(account);
            });
        }
    }
}