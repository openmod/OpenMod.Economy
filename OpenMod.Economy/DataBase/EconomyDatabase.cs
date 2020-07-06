#region

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Eventing;
using OpenMod.Economy.API;
using OpenMod.Economy.Events;

#endregion

namespace OpenMod.Economy.DataBase
{
    public sealed class EconomyDatabase : IEconomyDatabase
    {
        private readonly IEventBus m_EventBus;
        private readonly Economy m_Plugin;
        private readonly IStringLocalizer m_StringLocalizer;

        private IEconomyInternalDatabase m_Database;
        private bool m_IsDisposing;
        private SemaphoreSlim m_SemaphoreSlim;

        public EconomyDatabase(IConfiguration configuration, decimal defaultBalance,
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

        public async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var balance = await m_Database.GetBalanceAsync(ownerId, ownerType);

            var getBalanceEvent = new GetBalanceEvent(ownerId, ownerType, balance);
            await m_EventBus.EmitAsync(m_Plugin, this, getBalanceEvent);

            return balance;
        }

        public async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            if (amount == 0)
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount", amount]);

            var balance = await m_Database.UpdateBalanceAsync(ownerId, ownerType, amount);

            var getBalanceEvent = new ChangeBalanceEvent(ownerId, ownerType, balance, amount);
            await m_EventBus.EmitAsync(m_Plugin, this, getBalanceEvent);

            return balance;
        }

        public async Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            await m_Database.SetAccountAsync(ownerId, ownerType, balance);

            var getBalanceEvent = new SetAccountEvent(ownerId, ownerType, balance);
            await m_EventBus.EmitAsync(m_Plugin, this, getBalanceEvent);
        }
    }
}