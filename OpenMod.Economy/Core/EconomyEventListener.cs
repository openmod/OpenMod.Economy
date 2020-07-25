#region

using System.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.API.Prioritization;
using OpenMod.Core.Eventing;
using OpenMod.Core.Ioc;
using OpenMod.Economy.API;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Core
{
    public sealed class EconomyEventListener : IEventListener<OpenModInitializedEvent>
    {
        private readonly EconomyDatabaseController m_EconomyController;
        private readonly IEconomyDispatcher m_EconomyDispatcher;

        public EconomyEventListener(IEconomyProvider economyProvider, IEconomyDispatcher economyDispatcher)
        {
            m_EconomyController = economyProvider as EconomyDatabaseController;
            m_EconomyDispatcher = economyDispatcher;
        }

        [EventListener(IgnoreCancelled = true, Priority = Priority.Lowest)]
        public Task HandleEventAsync(object _, OpenModInitializedEvent __)
        {
            m_EconomyDispatcher.LoadDispatcher();
            return m_EconomyController?.LoadControllerAsync();
        }
    }
}