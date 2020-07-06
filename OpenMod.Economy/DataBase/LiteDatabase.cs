#region

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.Core.Commands;
using OpenMod.Economy.API;
using OpenMod.Economy.Core;
using OpenMod.Economy.Helpers;
using OpenMod.Extensions.Economy.Abstractions;

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

        public Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return ExecuteLiteDbContextAsync(db =>
            {
                var accounts = db.GetCollection<AccountBase>(m_TableName);
                var account = accounts.FindById(uniqueId);
                return account.Balance;
            });
        }

        public Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return ExecuteLiteDbContextAsync(db =>
            {
                var accounts = db.GetCollection<AccountBase>(m_TableName);
                var account = accounts.FindById(uniqueId) ?? new AccountBase
                {
                    UniqueId = uniqueId,
                    Balance = m_DefaultBalance
                };

                account.Balance += amount;
                if (account.Balance < 0)
                    throw new NotEnoughBalanceException(m_StringLocalizer["economy:fail:not_enough_balance",
                        account.Balance]);

                accounts.Upsert(account);
                return account.Balance;
            });
        }

        public Task SetAccountAsync(string ownerId, string ownerType, decimal balance)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return ExecuteLiteDbContextAsync(db =>
            {
                var accounts = db.GetCollection<AccountBase>(m_TableName);
                var account = accounts.FindById(uniqueId) ?? new AccountBase
                {
                    UniqueId = uniqueId
                };

                account.Balance = balance;
                accounts.Upsert(account);
            });
        }
    }
}