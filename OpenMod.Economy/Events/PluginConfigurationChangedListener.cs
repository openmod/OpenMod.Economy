using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenMod.API.Eventing;
using OpenMod.Core.Plugins.Events;
using OpenMod.Economy.API;
using OpenMod.Economy.Extensions;
using OpenMod.Economy.Models;

namespace OpenMod.Economy.Events;

[UsedImplicitly]
public sealed class PluginConfigurationChangedListener : IEventListener<PluginConfigurationChangedEvent>
{
    private readonly DatabaseSettings m_DatabaseSettings;
    private readonly EconomySettings m_EconomySettings;
    private readonly ILogger<Economy> m_Logger;

    public PluginConfigurationChangedListener(
        DatabaseSettings databaseSettings,
        EconomySettings economySettings,
        ILogger<Economy> logger)
    {
        m_DatabaseSettings = databaseSettings;
        m_EconomySettings = economySettings;
        m_Logger = logger;
    }

    public async Task HandleEventAsync(object? sender, PluginConfigurationChangedEvent @event)
    {
        if (@event.Plugin is not Economy)
            return;

        var oldDbType = m_DatabaseSettings.DbType;
        @event.Configuration.BindConfig("database", m_DatabaseSettings, m_Logger);
        m_DatabaseSettings.ConnectionString =
            m_DatabaseSettings.ConnectionString.Replace("{WorkingDirectory}", @event.Plugin.WorkingDirectory);

        if (oldDbType != m_DatabaseSettings.DbType)
        {
            m_Logger.LogWarning("Changing DB types during runtime({0} -> {1}) (Old database will not be exported).",
                oldDbType, m_DatabaseSettings.DbType);

            var database = @event.Plugin.LifetimeScope.Resolve<IDatabase>();
            await database.CheckSchemasAsync();
        }

        @event.Configuration.BindConfig("economy", m_EconomySettings, m_Logger);
    }
}