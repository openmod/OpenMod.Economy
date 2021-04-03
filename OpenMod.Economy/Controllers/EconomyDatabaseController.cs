#region

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OpenMod.API;
using OpenMod.API.Commands;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Plugins;
using OpenMod.Core.Ioc;
using OpenMod.Economy.API;
using OpenMod.Economy.DataBase;
using OpenMod.Economy.Events;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Controllers
{
    [ServiceImplementation]
    [UsedImplicitly]
    public sealed class EconomyDatabaseController : DatabaseController, IEconomyProvider
    {
        private readonly IEventBus m_EventBus;
        private readonly IOpenModComponent m_OpenModComponent;

        private IEconomyProvider m_Database;

        public EconomyDatabaseController(IEventBus eventBus,
            IOpenModComponent openModComponent,
            IPluginAccessor<Economy> plugin) : base(plugin)
        {
            m_EventBus = eventBus;
            m_OpenModComponent = openModComponent;
        }

        protected override Task LoadControllerAsync()
        {
            return IsServiceLoaded ? Task.CompletedTask : CreateDatabaseProvider();
        }

        protected override Task ConfigurationChangedAsync()
        {
            return IsServiceLoaded ? CreateDatabaseProvider() : Task.CompletedTask;
        }

        private Task CreateDatabaseProvider()
        {
            var dataBaseType = DbStoreType switch
            {
                StoreType.DataStore => typeof(DataStoreDatabase),
                StoreType.LiteDb => typeof(LiteDbDatabase),
                StoreType.MySql => typeof(MySqlDatabase),
                StoreType.UserData => typeof(UserDataDatabase),
                _ => throw new ArgumentOutOfRangeException(nameof(DbStoreType), DbStoreType, null)
            };

            // ReSharper disable once PossibleNullReferenceException
            m_Database =
                ActivatorUtilitiesEx.CreateInstance(Plugin.Instance.LifetimeScope, dataBaseType) as IEconomyProvider;
            return m_Database is MySqlDatabase mySqlDatabase ? mySqlDatabase.CheckShemasAsync() : Task.CompletedTask;
        }

        #region Economy

        public string CurrencyName => m_Database.CurrencyName;
        public string CurrencySymbol => m_Database.CurrencySymbol;

        public async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            await LoadControllerBaseAsync();
            var balance = await m_Database.GetBalanceAsync(ownerId, ownerType);

            var getBalanceEvent = new GetBalanceEvent(ownerId, ownerType, balance);
            await m_EventBus.EmitAsync(m_OpenModComponent, this, getBalanceEvent);

            return balance;
        }

        public async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string reason)
        {
            await LoadControllerBaseAsync();
            if (amount == 0)
                throw new UserFriendlyException(StringLocalizer["economy:fail:invalid_amount",
                    new {Amount = amount}]);

            var oldBalance = await m_Database.GetBalanceAsync(ownerId, ownerType);
            var balance = await m_Database.UpdateBalanceAsync(ownerId, ownerType, amount, reason);

            var getBalanceEvent = new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, balance, reason);
            await m_EventBus.EmitAsync(m_OpenModComponent, this, getBalanceEvent);

            return balance;
        }

        public async Task SetBalanceAsync(string ownerId, string ownerType, decimal newBalance)
        {
            await LoadControllerBaseAsync();
            var oldBalance = await m_Database.GetBalanceAsync(ownerId, ownerType);
            await m_Database.SetBalanceAsync(ownerId, ownerType, newBalance);

            var getBalanceEvent =
                new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, newBalance, "Set Balance Requested");
            await m_EventBus.EmitAsync(m_OpenModComponent, this, getBalanceEvent);
        }

        #endregion
    }
}