#region

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API.Eventing;
using OpenMod.Core.Commands;
using OpenMod.Economy.API;
using OpenMod.Economy.Events;

#endregion

namespace OpenMod.Economy.DataBase
{
    public sealed class Database : IEconomyDatabase
    {
        private readonly IEventBus m_EventBus;
        private readonly Economy m_Plugin;
        private readonly IStringLocalizer m_StringLocalizer;

        private IEconomyInternalDatabase m_Database;
        private bool m_IsDisposing;
        private SemaphoreSlim m_SemaphoreSlim;

        public Database(bool allowNegativeBalance, decimal defaultBalance, IEventBus eventBus, Economy plugin,
            IServiceProvider serviceProvider, StoreType storeType, IStringLocalizer stringLocalizer)
        {
            AllowNegativeBalance = allowNegativeBalance;
            DefaultBalance = defaultBalance;

            m_Plugin = plugin;
            m_EventBus = eventBus;
            m_StringLocalizer = stringLocalizer;

            m_SemaphoreSlim = new SemaphoreSlim(1, 1);
            var dataBaseType = storeType switch
            {
                StoreType.DataStore => typeof(MySqlDatabase),
                StoreType.LiteDb => typeof(LiteDatabase),
                StoreType.MySql => typeof(MySqlDatabase),
                StoreType.UserData => typeof(UserDataDatabase),
                _ => throw new ArgumentOutOfRangeException(nameof(storeType), storeType, null)
            };
            m_Database =
                ActivatorUtilities.CreateInstance(serviceProvider, dataBaseType, DefaultBalance) as
                    IEconomyInternalDatabase;
        }

        public bool AllowNegativeBalance { get; }
        public decimal DefaultBalance { get; }

        public async Task LoadDatabaseAsync()
        {
            if (!(m_Database is MySqlDatabase mySqlDatabase))
                return;

            await mySqlDatabase.CheckShemasAsync();
        }

        public Task<bool> CreateUserAccountAsync(string userId, string userType)
        {
            return m_Database.CreateUserAccountAsync(userId, userType);
        }

        public async Task<decimal> GetBalanceAsync(string userId, string userType)
        {
            var balance = await m_Database.GetBalanceAsync(userId, userType);

            var getBalanceEvent = new GetBalanceEvent(userId, userType, balance);
            await m_EventBus.EmitAsync(m_Plugin, this, getBalanceEvent);

            return balance;
        }

        public async Task<decimal> IncreaseBalanceAsync(string userId, string userType, decimal amount)
        {
            if (amount < 0)
                amount = Math.Abs(amount);
            else if (amount == 0)
                throw new UserFriendlyException(m_StringLocalizer["uconomy:fail:invalid_amount", amount]);

            var balance = await m_Database.IncreaseBalanceAsync(userId, userType, amount);

            var getBalanceEvent = new ChangeBalanceEvent(userId, userType, balance, amount);
            await m_EventBus.EmitAsync(m_Plugin, this, getBalanceEvent);

            return balance;
        }

        public async Task<decimal> DecreaseBalanceAsync(string userId, string userType, decimal amount,
            bool? allowNegativeBalance = null)
        {
            if (amount < 0)
                amount = Math.Abs(amount);
            else if (amount == 0)
                throw new UserFriendlyException(m_StringLocalizer["uconomy:fail:invalid_amount", amount]);

            allowNegativeBalance ??= AllowNegativeBalance;

            var balance = await m_Database.DecreaseBalanceAsync(userId, userType, amount, allowNegativeBalance.Value);

            var getBalanceEvent = new ChangeBalanceEvent(userId, userType, balance, -amount);
            await m_EventBus.EmitAsync(m_Plugin, this, getBalanceEvent);

            return balance;
        }

        public ValueTask DisposeAsync()
        {
            if (m_IsDisposing)
                return new ValueTask(Task.CompletedTask);

            if (m_Database is MySqlDatabase mySqlDatabase)
                mySqlDatabase.Dispose();

            m_Database = null;
            m_SemaphoreSlim.Dispose();
            m_SemaphoreSlim = null;
            m_IsDisposing = true;
            return new ValueTask(Task.CompletedTask);
        }
    }
}