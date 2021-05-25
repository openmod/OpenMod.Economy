#region

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Commands;
using OpenMod.API.Ioc;
using OpenMod.Core.Ioc;
using OpenMod.Economy.API;
using OpenMod.Economy.DataBase;
using OpenMod.Economy.Events;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Controllers
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    [UsedImplicitly]
    public sealed class EconomyDatabaseController : DatabaseController, IEconomyProvider
    {
        private IEconomyProvider m_Database;

        protected override Task LoadControllerAsync()
        {
            return CreateDatabaseProvider();
        }

        internal override Task ConfigurationChangedAsync()
        {
            return CreateDatabaseProvider();
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
            m_Database = ActivatorUtilitiesEx.CreateInstance(LifetimeScope, dataBaseType) as IEconomyProvider;
            return m_Database is MySqlDatabase mySqlDatabase ? mySqlDatabase.CheckShemasAsync() : Task.CompletedTask;
        }

        #region Economy

        public string CurrencyName => Configuration.GetSection("Economy:CurrencyName").Get<string>();
        public string CurrencySymbol => Configuration.GetSection("Economy:CurrencySymbol").Get<string>();

        public async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            var balance = await m_Database.GetBalanceAsync(ownerId, ownerType);

            var getBalanceEvent = new GetBalanceEvent(ownerId, ownerType, balance);
            await EventBus.EmitAsync(OpenModComponent, this, getBalanceEvent);

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
            await EventBus.EmitAsync(OpenModComponent, this, getBalanceEvent);

            return balance;
        }

        public async Task SetBalanceAsync(string ownerId, string ownerType, decimal newBalance)
        {
            var oldBalance = await m_Database.GetBalanceAsync(ownerId, ownerType);
            await m_Database.SetBalanceAsync(ownerId, ownerType, newBalance);

            var getBalanceEvent =
                new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, newBalance, "Set Balance Requested");
            await EventBus.EmitAsync(OpenModComponent, this, getBalanceEvent);
        }

        #endregion
    }
}