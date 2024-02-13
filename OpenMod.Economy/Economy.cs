using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
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
public sealed class Economy(
    DatabaseSettings databaseSettings,
    IServiceProvider serviceProvider)
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
    : OpenModUniversalPlugin(serviceProvider)
#pragma warning restore CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
{
    protected override async Task OnLoadAsync()
    {
        databaseSettings.ConnectionString =
            databaseSettings.ConnectionString.Replace("{WorkingDirectory}", WorkingDirectory);

        var database = serviceProvider.GetRequiredService<IDatabase>();
        await database.CheckSchemasAsync();

        Logger.LogInformation($"Database type set to: '{databaseSettings.DbType}'");
    }
}