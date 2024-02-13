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
public sealed class PluginConfigurationChangedListener(
    DatabaseSettings databaseSettings,
    EconomySettings economySettings,
    ILogger<Economy> logger) : IEventListener<PluginConfigurationChangedEvent>
{
    public async Task HandleEventAsync(object? sender, PluginConfigurationChangedEvent @event)
    {
        if (@event.Plugin is not Economy)
            return;

        var oldDbType = databaseSettings.DbType;
        @event.Configuration.BindConfig("database", databaseSettings, logger);
        databaseSettings.ConnectionString =
            databaseSettings.ConnectionString.Replace("{WorkingDirectory}", @event.Plugin.WorkingDirectory);

        if (oldDbType != databaseSettings.DbType)
        {
            logger.LogWarning("Changing DB types during runtime({0} -> {1}) (Old database will not be exported).",
                oldDbType, databaseSettings.DbType);

            var database = @event.Plugin.LifetimeScope.Resolve<IDatabase>();
            await database.CheckSchemasAsync();
        }

        @event.Configuration.BindConfig("economy", economySettings, logger);
    }
}