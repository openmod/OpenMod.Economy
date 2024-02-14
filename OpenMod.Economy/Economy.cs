using System;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;
using OpenMod.Economy.API;
using OpenMod.Economy.Models;

[assembly:
    PluginMetadata("OpenMod.Economy", Author = "OpenMod,Rube200", DisplayName = "OpenMod.Economy",
        Website = "https://github.com/openmod/OpenMod.Economy")]

namespace OpenMod.Economy;

[UsedImplicitly]
public sealed class Economy : OpenModUniversalPlugin
{
    private readonly DatabaseSettings m_DatabaseSettings;

    public Economy(DatabaseSettings databaseSettings, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        m_DatabaseSettings = databaseSettings;
    }

    protected override async Task OnLoadAsync()
    {
        m_DatabaseSettings.ConnectionString =
            m_DatabaseSettings.ConnectionString.Replace("{WorkingDirectory}", WorkingDirectory);

        var database = LifetimeScope.Resolve<IDatabase>();
        await database.CheckSchemasAsync();

        Logger.LogInformation($"Database type set to: '{m_DatabaseSettings.DbType}'");
    }
}