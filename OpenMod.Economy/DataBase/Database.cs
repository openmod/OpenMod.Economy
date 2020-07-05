#region

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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

        public Database(IConfiguration configuration, decimal defaultBalance,
            IEventBus eventBus, Economy plugin,
            IServiceProvider serviceProvider, StoreType storeType, IStringLocalizer stringLocalizer)
        {
            DefaultBalance = defaultBalance;

            m_Plugin = plugin;
            m_EventBus = eventBus;
            m_StringLocalizer = stringLocalizer;

            m_SemaphoreSlim = new SemaphoreSlim(1, 1);

            var parameters = new List<object>
            {
                DefaultBalance,
                configuration["Table_Name"]
            };
            var dataBaseType = storeType switch
            {
                StoreType.DataStore => typeof(DataStoreDatabase),
                StoreType.LiteDb => typeof(LiteDatabase),
                StoreType.MySql => typeof(MySqlDatabase),
                StoreType.UserData => typeof(UserDataDatabase),
                _ => throw new ArgumentOutOfRangeException(nameof(storeType), storeType, null)
            };

            m_Database =
                ActivatorUtilities.CreateInstance(serviceProvider, dataBaseType, parameters.ToArray()) as
                    IEconomyInternalDatabase;
        }

        public decimal DefaultBalance { get; }

        public async Task LoadDatabaseAsync()
        {
            if (!(m_Database is MySqlDatabase mySqlDatabase))
                return;

            await mySqlDatabase.CheckShemasAsync();
        }

        public async Task<decimal> GetBalanceAsync(IAccountId accountId)
        {
            var balance = await m_Database.GetBalanceAsync(accountId);

            var getBalanceEvent = new GetBalanceEvent(accountId, balance);
            await m_EventBus.EmitAsync(m_Plugin, this, getBalanceEvent);

            return balance;
        }

        public async Task<decimal> UpdateBalanceAsync(IAccountId accountId, decimal amount)
        {
            if (amount == 0)
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount", amount]);

            var balance = await m_Database.UpdateBalanceAsync(accountId, amount);

            var getBalanceEvent = new ChangeBalanceEvent(accountId, balance, amount);
            await m_EventBus.EmitAsync(m_Plugin, this, getBalanceEvent);

            return balance;
        }

        public async Task SetAccountAsync(IAccountId accountId, decimal balance)
        {
            await m_Database.SetAccountAsync(accountId, balance);

            var getBalanceEvent = new SetAccountEvent(accountId, balance);
            await m_EventBus.EmitAsync(m_Plugin, this, getBalanceEvent);
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