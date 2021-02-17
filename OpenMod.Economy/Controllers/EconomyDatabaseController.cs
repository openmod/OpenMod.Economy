#region

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;
using OpenMod.API.Commands;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Plugins;
using OpenMod.Economy.API;
using OpenMod.Economy.Database;
using OpenMod.Economy.Events;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Controllers
{
    [PluginServiceImplementation]
    [UsedImplicitly]
    public sealed class EconomyDatabaseController : DatabaseController, IEconomyProvider
    {
        private readonly IEventBus m_EventBus;
        private readonly IServiceProvider m_ServiceProvider;

        private IEconomyProvider m_Database;

        public EconomyDatabaseController(IEventBus eventBus,
            IPluginAccessor<Economy> economyPlugin,
            IServiceProvider serviceProvider) : base(economyPlugin)
        {
            m_EventBus = eventBus;
            m_ServiceProvider = serviceProvider;
            AsyncContext.Run(async () => await LoadControllerBaseAsync());
        }

        protected override Task ConfigurationChangedAsync()
        {
            if (!IsServiceLoaded)
                return Task.CompletedTask;

            var dataBaseType = DbStoreType switch
            {
                StoreType.DataStore => typeof(DataStoreDatabase),
                StoreType.LiteDb => typeof(LiteDbDatabase),
                StoreType.MySql => typeof(MySqlDatabase),
                StoreType.UserData => typeof(UserDataDatabase),
                _ => throw new ArgumentOutOfRangeException(nameof(DbStoreType), DbStoreType, null)
            };

            m_Database = ActivatorUtilities.CreateInstance(m_ServiceProvider, dataBaseType) as IEconomyProvider;
            if (!(m_Database is MySqlDatabase mySqlDatabase))
                return Task.CompletedTask;

            return mySqlDatabase.CheckShemasAsync();
        }

        protected override Task LoadControllerAsync()
        {
            if (IsServiceLoaded)
                return Task.CompletedTask;

            var dataBaseType = DbStoreType switch
            {
                StoreType.DataStore => typeof(DataStoreDatabase),
                StoreType.LiteDb => typeof(LiteDbDatabase),
                StoreType.MySql => typeof(MySqlDatabase),
                StoreType.UserData => typeof(UserDataDatabase),
                _ => throw new ArgumentOutOfRangeException(nameof(DbStoreType), DbStoreType, null)
            };

            m_Database = ActivatorUtilities.CreateInstance(m_ServiceProvider, dataBaseType) as IEconomyProvider;
            if (!(m_Database is MySqlDatabase mySqlDatabase))
                return Task.CompletedTask;

            return mySqlDatabase.CheckShemasAsync();
        }

        #region Economy

        public string CurrencyName => m_Database.CurrencyName;
        public string CurrencySymbol => m_Database.CurrencySymbol;

        public async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var balance = await m_Database.GetBalanceAsync(ownerId, ownerType);

            var getBalanceEvent = new GetBalanceEvent(ownerId, ownerType, balance);
            await m_EventBus.EmitAsync(EconomyPlugin.Instance!, this, getBalanceEvent);

            return balance;
        }

        public async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string reason)
        {
            if (amount == 0)
                throw new UserFriendlyException(StringLocalizer["economy:fail:invalid_amount",
                    new {Amount = amount}]);

            var oldBalance = await m_Database.GetBalanceAsync(ownerId, ownerType);
            var balance = await m_Database.UpdateBalanceAsync(ownerId, ownerType, amount, reason);

            var getBalanceEvent = new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, balance, reason);
            await m_EventBus.EmitAsync(EconomyPlugin.Instance!, this, getBalanceEvent);

            return balance;
        }

        public async Task SetBalanceAsync(string ownerId, string ownerType, decimal newBalance)
        {
            var oldBalance = await m_Database.GetBalanceAsync(ownerId, ownerType);
            await m_Database.SetBalanceAsync(ownerId, ownerType, newBalance);

            var getBalanceEvent =
                new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, newBalance, "Set Balance Requested");
            await m_EventBus.EmitAsync(EconomyPlugin.Instance!, this, getBalanceEvent);
        }

        #endregion
    }
}