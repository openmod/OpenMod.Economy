#region

using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LiteDB;
using OpenMod.API.Plugins;
using OpenMod.Economy.Core;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class LiteDbDatabase : DataBaseCore
    {
        public LiteDbDatabase(IPluginAccessor<Economy> economyPlugin) : base(economyPlugin)
        {
        }

        private string m_ConnectionString => EconomyPlugin.Instance.Configuration["Connection_String"];

        public override async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            try
            {
                await UniTask.SwitchToMainThread();
                using var liteDb = new LiteDatabase(m_ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId);
                return account?.Balance ?? DefaultBalance;
            }
            finally
            {
                await UniTask.Yield();
            }
        }

        public override async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            try
            {
                await UniTask.SwitchToMainThread();
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
                return account.Balance;
            }
            finally
            {
                await UniTask.Yield();
            }
        }

        public override async Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            try
            {
                await UniTask.SwitchToMainThread();
                using var liteDb = new LiteDatabase(m_ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId) ?? new AccountBase
                {
                    UniqueId = uniqueId
                };

                account.Balance = balance;
                accounts.Upsert(account);
            }
            finally
            {
                await UniTask.Yield();
            }
        }
    }
}