#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Core.Console;
using OpenMod.Core.Plugins;
using OpenMod.Economy.Controllers;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

[assembly:
    PluginMetadata("OpenMod.Economy", Author = "OpenMod,Rube200", DisplayName = "OpenMod.Economy",
        Website = "https://github.com/openmod/OpenMod.Economy")]

namespace OpenMod.Economy
{
    public sealed class Economy : OpenModUniversalPlugin
    {
        private readonly IConsoleActorAccessor m_ConsoleActorAccessor;
        private readonly IEconomyProvider m_EconomyProvider;

        public Economy(IConsoleActorAccessor consoleActorAccessor,
            IEconomyProvider economyProvider,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_ConsoleActorAccessor = consoleActorAccessor;
            m_EconomyProvider = economyProvider;
        }

        protected override async Task OnLoadAsync()
        {
            if (m_EconomyProvider is not DatabaseController databaseController)
                return;

            await databaseController.InjectAndLoad(LifetimeScope);
            Logger.LogInformation($"Database type set to: '{databaseController.DbStoreType}'");

            await m_EconomyProvider.GetBalanceAsync(m_ConsoleActorAccessor.Actor.Type,
                m_ConsoleActorAccessor.Actor.Id); //force call to detect missing libs
        }
    }
}