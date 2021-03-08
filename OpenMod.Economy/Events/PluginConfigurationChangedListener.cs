#region

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenMod.API.Eventing;
using OpenMod.Core.Plugins.Events;
using OpenMod.Economy.API;
using OpenMod.Economy.Controllers;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Events
{
    [UsedImplicitly]
    public sealed class PluginConfigurationChangedListener : IEventListener<PluginConfigurationChangedEvent>
    {
        private readonly IEconomyProvider m_EconomyProvider;
        private readonly ILogger<Economy> m_Logger;

        public PluginConfigurationChangedListener(IEconomyProvider economyProvider, ILogger<Economy> logger)
        {
            m_EconomyProvider = economyProvider;
            m_Logger = logger;
        }

        public Task HandleEventAsync(object sender, PluginConfigurationChangedEvent @event)
        {
            if (@event.Plugin is not Economy)
                return Task.CompletedTask;

            if (m_EconomyProvider is not DatabaseController databaseController)
                return Task.CompletedTask;

            var storeType = @event.Configuration.GetSection("Database:Store_Type").Get<string>();
            var dbStoreType = Enum.TryParse<StoreType>(storeType, true, out var dbType) ? dbType : StoreType.DataStore;
            m_Logger.LogInformation($"Config changed! Database type set to: '{dbStoreType}'");

            return databaseController.ConfigurationChangedBaseAsync();
        }
    }
}