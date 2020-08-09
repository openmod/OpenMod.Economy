#region

using System.IO;
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

        private string ConnectionString => EconomyPlugin.Instance.Configuration["Connection_String"].Replace("{WorkingDirectory}", EconomyPlugin.Instance.WorkingDirectory);

        public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(() =>
            {
                using var liteDb = new LiteDatabase(ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId);
                tcs.SetResult(account?.Balance ?? DefaultBalance);
                return Task.CompletedTask;
            });

            return tcs.Task;
        }

        public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(() =>
            {
                using var liteDb = new LiteDatabase(ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId) ?? new AccountBase
                {
                    UniqueId = uniqueId,
                    Balance = DefaultBalance
                };

                var newBalance = account.Balance + amount;
                if (newBalance < 0)
                    throw new NotEnoughBalanceException(StringLocalizer["economy:fail:not_enough_balance", new { account.Balance, CurrencySymbol }], account.Balance);

                account.Balance = newBalance;
                accounts.Upsert(account);
                tcs.SetResult(newBalance);
                return Task.CompletedTask;
            });

            return tcs.Task;
        }

        public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            var tcs = new TaskCompletionSource<decimal>();

            m_EconomyDispatcher.Enqueue(() =>
            {
                using var liteDb = new LiteDatabase(ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId) ?? new AccountBase
                {
                    UniqueId = uniqueId
                };

                account.Balance = balance;
                accounts.Upsert(account);
                return Task.CompletedTask;
            });

            return tcs.Task;
        }
    }
}