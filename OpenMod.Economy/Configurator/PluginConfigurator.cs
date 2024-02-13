using System;
using Autofac;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Plugins;
using OpenMod.Economy.API;
using OpenMod.Economy.DataBase;
using OpenMod.Economy.Extensions;
using OpenMod.Economy.Models;

namespace OpenMod.Economy.Configurator;

[UsedImplicitly]
public class PluginConfigurator : IPluginContainerConfigurator
{
    public void ConfigureContainer(IPluginServiceConfigurationContext context)
    {
        context.ContainerBuilder.RegisterConfig<DatabaseSettings>();
        context.ContainerBuilder.RegisterConfig<EconomySettings>();

        context.ContainerBuilder.Register<IServiceProvider, DatabaseSettings, IDatabase>(
            (serviceProvider, databaseSettings) =>
            {
                var dbType = databaseSettings.DbType switch
                {
                    StoreType.DataStore => typeof(DataStoreDatabase),
                    StoreType.LiteDb => typeof(LiteDatabase),
                    StoreType.MySql => typeof(MySqlDatabase),
                    StoreType.UserData => typeof(UserDataDatabase),
                    _ => typeof(DataStoreDatabase)
                };

                return (IDatabase)ActivatorUtilities.CreateInstance(serviceProvider, dbType);
            });
    }
}