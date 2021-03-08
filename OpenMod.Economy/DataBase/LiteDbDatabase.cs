#region

using System.Threading.Tasks;
using Autofac;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.Economy.API;
using OpenMod.Economy.Classes;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class LiteDbDatabase : EconomyDatabaseCore
    {
        private readonly IEconomyDispatcher m_Dispatcher;
        private readonly IStringLocalizer m_StringLocalizer;

        private readonly string m_WorkingDirectory;
        //private readonly Lazy<IPluginAccessor<Economy> m_LazyAccessor;

        public LiteDbDatabase(IConfiguration configuration, IEconomyDispatcher dispatcher,
            IComponentContext lifetimeScope, IStringLocalizer stringLocalizer) : base(configuration)
        {
            m_Dispatcher = dispatcher;
            m_StringLocalizer = stringLocalizer;

            m_WorkingDirectory = lifetimeScope.Resolve<Economy>().WorkingDirectory;
        }

        private string ConnectionString => Configuration.GetSection("Database:Connection_String")
            .Get<string>().Replace("{WorkingDirectory}", m_WorkingDirectory);

        public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return m_Dispatcher.EnqueueV2(() =>
            {
                using var liteDb = new LiteDatabase(ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId);
                return account?.Balance ?? DefaultBalance;
            });
        }

        public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string _)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return m_Dispatcher.EnqueueV2(() =>
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
                    throw new NotEnoughBalanceException(
                        m_StringLocalizer["economy:fail:not_enough_balance",
                            new {account.Balance, EconomyProvider = (IEconomyProvider) this}],
                        account.Balance);

                account.Balance = newBalance;
                accounts.Upsert(account);
                return newBalance;
            });
        }

        public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            var uniqueId = $"{ownerType}_{ownerId}";
            return m_Dispatcher.EnqueueV2(() =>
            {
                using var liteDb = new LiteDatabase(ConnectionString);
                var accounts = liteDb.GetCollection<AccountBase>(TableName);
                var account = accounts.FindById(uniqueId) ?? new AccountBase {UniqueId = uniqueId};
                account.Balance = balance;
                accounts.Upsert(account);
            });
        }
    }
}