#region

using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenMod.API.Eventing;
using OpenMod.Core.Plugins.Events;
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

        public async Task HandleEventAsync(object sender, PluginConfigurationChangedEvent @event)
        {
            if (@event.Plugin is not Economy)
                return;

            if (m_EconomyProvider is not DatabaseController databaseController)
                return;

            await databaseController.ConfigurationChangedAsync();
            m_Logger.LogInformation($"Config changed! Database type set to: '{databaseController.DbStoreType}'");
        }
    }
}