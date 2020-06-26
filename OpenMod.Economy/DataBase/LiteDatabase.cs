#region

using System.Threading.Tasks;
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

        internal LiteDatabase(decimal defaultBalance, string liteDbString, IStringLocalizer stringLocalizer,
            string tableName) : base(liteDbString)
        {
            m_DefaultBalance = defaultBalance;
            m_StringLocalizer = stringLocalizer;
            m_TableName = tableName;
        }

        public Task<bool> CreateUserAccountAsync(string userId, string userType)
        {
            var uniqueId = $"{userType}_{userId}";
            return ExecuteLiteDbAsync(db =>
            {
                var accounts = db.GetCollection<UserAccount>(m_TableName);
                var account = accounts.FindById(uniqueId);

                if (account != null)
                    return false;

                return accounts.Insert(new[]
                {
                    new UserAccount
                    {
                        Balance = m_DefaultBalance,
                        UniqueId = uniqueId
                    }
                }) >= 1;
            });
        }

        public Task<decimal> GetBalanceAsync(string userId, string userType)
        {
            var uniqueId = $"{userType}_{userId}";
            return ExecuteLiteDbAsync(db =>
            {
                var accounts = db.GetCollection<UserAccount>(m_TableName);
                var account = accounts.FindById(uniqueId);
                return account.Balance;
            });
        }

        public Task<decimal> IncreaseBalanceAsync(string userId, string userType, decimal amount)
        {
            var uniqueId = $"{userType}_{userId}";
            return ExecuteLiteDbAsync(db =>
            {
                var accounts = db.GetCollection<UserAccount>(m_TableName);
                var account = accounts.FindById(uniqueId);
                var balance = account.Balance += amount;

                accounts.Update(account);
                return balance;
            });
        }

        public Task<decimal> DecreaseBalanceAsync(string userId, string userType, decimal amount,
            bool allowNegativeBalance)
        {
            var uniqueId = $"{userType}_{userId}";
            return ExecuteLiteDbAsync(db =>
            {
                var accounts = db.GetCollection<UserAccount>(m_TableName);
                var account = accounts.FindById(uniqueId);

                if (!allowNegativeBalance && amount > account.Balance)
                    throw new UserFriendlyException(m_StringLocalizer["uconomy:fail:not_enough_balance",
                        account.Balance]);

                var balance = account.Balance -= amount;
                accounts.Update(account);
                return balance;
            });
        }
    }
}