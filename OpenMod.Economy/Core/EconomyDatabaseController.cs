#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Plugins;
using OpenMod.Economy.API;
using OpenMod.Economy.DataBase;
using OpenMod.Economy.Events;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Core
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public sealed class EconomyDatabaseController : IEconomyProvider
    {
        private readonly IPluginAccessor<Economy> m_EconomyPlugin;
        private readonly IEventBus m_EventBus;
        private readonly IServiceProvider m_ServiceProvider;
        private IEconomyProvider m_Database;

        public EconomyDatabaseController(IPluginAccessor<Economy> economyPlugin, IEventBus eventBus,
            IServiceProvider serviceProvider)
        {
            m_EconomyPlugin = economyPlugin;
            m_EventBus = eventBus;
            m_ServiceProvider = serviceProvider;
        }

        public string CurrencyName => m_Database.CurrencyName;
        public string CurrencySymbol => m_Database.CurrencySymbol;
        private IStringLocalizer m_StringLocalizer => m_EconomyPlugin.Instance.StringLocalizer;

        public async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var balance = await m_Database.GetBalanceAsync(ownerId, ownerType);
            var getBalanceEvent = new GetBalanceEvent(ownerId, ownerType, balance);
            await m_EventBus.EmitAsync(m_EconomyPlugin.Instance, this, getBalanceEvent);

            return balance;
        }

        public async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            if (amount == 0)
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount", new {amount}]);

            var balance = await m_Database.UpdateBalanceAsync(ownerId, ownerType, amount);

            var getBalanceEvent = new ChangeBalanceEvent(ownerId, ownerType, balance, amount);
            await m_EventBus.EmitAsync(m_EconomyPlugin.Instance, this, getBalanceEvent);

            return balance;
        }

        public async Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            await m_Database.SetBalanceAsync(ownerId, ownerType, balance);

            var getBalanceEvent = new SetAccountEvent(ownerId, ownerType, balance);
            await m_EventBus.EmitAsync(m_EconomyPlugin.Instance, this, getBalanceEvent);
        }

        public async Task LoadControllerAsync()
        {
            if (m_EconomyPlugin.Instance == null)
                return;

            Enum.TryParse<StoreType>(m_EconomyPlugin.Instance.Configuration["Store_Type"], true, out var storeType);
            var dataBaseType = storeType switch
            {
                StoreType.DataStore => typeof(DataStoreDatabase),
                StoreType.LiteDb => typeof(LiteDbDatabase),
                StoreType.MySql => typeof(MySqlDatabase),
                StoreType.UserData => typeof(UserDataDatabase),
                _ => throw new ArgumentOutOfRangeException(nameof(storeType), storeType, null)
            };

            m_Database = ActivatorUtilities.CreateInstance(m_ServiceProvider, dataBaseType) as IEconomyProvider;
            if (!(m_Database is MySqlDatabase mySqlDatabase))
                return;

            await mySqlDatabase.CheckShemasAsync();
        }
    }
}