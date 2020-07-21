#region

using System.Threading.Tasks;
using LiteDB;
using OpenMod.API.Plugins;
using OpenMod.Economy.API;
using OpenMod.Economy.Core;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class LiteDbDatabase : DataBaseCore
    {
        private readonly IEconomyDispatcher m_EconomyDispatcher;

        public LiteDbDatabase(IEconomyDispatcher dispatcher, IPluginAccessor<Economy> economyPlugin) : base(
            economyPlugin)
        {
            m_EconomyDispatcher = dispatcher;
        }

        private string m_ConnectionString => EconomyPlugin.Instance.Configuration["Connection_String"];

        public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(() =>
            {
                using var liteDb = new LiteDatabase(m_ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId);
                tcs.SetResult(account?.Balance ?? DefaultBalance);
            });

            return tcs.Task;
        }

        public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(() =>
            {
                using var liteDb = new LiteDatabase(m_ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId) ?? new AccountBase
                {
                    UniqueId = uniqueId,
                    Balance = DefaultBalance
                };

                account.Balance += amount;
                if (account.Balance < 0)
                    throw new NotEnoughBalanceException(StringLocalizer["economy:fail:not_enough_balance",
                        account.Balance]);

                accounts.Upsert(account);
                tcs.SetResult(account.Balance);
            });

            return tcs.Task;
        }

        public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(() =>
            {
                using var liteDb = new LiteDatabase(m_ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId) ?? new AccountBase
                {
                    UniqueId = uniqueId
                };

                account.Balance = balance;
                accounts.Upsert(account);
            });

            return tcs.Task;
        }
    }
}