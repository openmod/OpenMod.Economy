using System.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.API.Prioritization;
using OpenMod.Core.Eventing;
using OpenMod.Core.Ioc;
using OpenMod.Economy.API;

namespace OpenMod.Economy.Core
{
    public sealed class EconomyEventListener : IEventListener<OpenModInitializedEvent>
    {
        private readonly IEconomyController m_EconomyController;
        private readonly IEconomyDispatcher m_EconomyDispatcher;

        public EconomyEventListener(IEconomyController economyController, IEconomyDispatcher economyDispatcher)
        {
            m_EconomyController = economyController;
            m_EconomyDispatcher = economyDispatcher;
        }

        [EventListener(IgnoreCancelled = true, Priority = Priority.Lowest)]
        public Task HandleEventAsync(object _, OpenModInitializedEvent __)
        {
            m_EconomyDispatcher.LoadDispatcher();
            return m_EconomyController.LoadDatabaseControllerAsync();
        }
    }
}
