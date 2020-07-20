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
using OpenMod.Economy.Events;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Transient)]
    public sealed class EconomyDatabase : IEconomyDatabase
    {
        private readonly IEconomyProvider m_Database;
        private readonly IPluginAccessor<Economy> m_EconomyPlugin;
        private readonly IEventBus m_EventBus;

        public EconomyDatabase(IEventBus eventBus, IPluginAccessor<Economy> economyPlugin,
            IServiceProvider serviceProvider)
        {
            m_EconomyPlugin = economyPlugin;
            m_EventBus = eventBus;

            var storeType = m_EconomyPlugin.Instance.DataStoreType;
            var dataBaseType = m_EconomyPlugin.Instance.DataStoreType switch
            {
                StoreType.DataStore => typeof(DataStoreDatabase),
                StoreType.LiteDb => typeof(LiteDbDatabase),
                StoreType.MySql => typeof(MySqlDatabase),
                StoreType.UserData => typeof(UserDataDatabase),
                _ => throw new ArgumentOutOfRangeException(nameof(storeType), storeType, null)
            };

            m_Database =
                ActivatorUtilities.CreateInstance(serviceProvider, dataBaseType) as IEconomyProvider;
        }

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
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount", amount]);

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

        public async Task LoadDatabaseAsync()
        {
            if (!(m_Database is MySqlDatabase mySqlDatabase))
                return;

            await mySqlDatabase.CheckShemasAsync();
        }
    }
}