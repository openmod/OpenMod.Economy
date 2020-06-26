#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.Core.Eventing;
using OpenMod.Core.Plugins;
using OpenMod.Core.Users.Events;
using OpenMod.Economy.API;

#endregion

[assembly: PluginMetadata("EconomyPlugin", Author = "OpenMod", DisplayName = "Economy")]

namespace OpenMod.Economy
{
    public sealed class Economy : OpenModUniversalPlugin
    {
        private readonly IServiceProvider m_ServiceProvider;
        private readonly IStringLocalizer m_StringLocalizer;

        public Economy(IServiceProvider serviceProvider, IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_ServiceProvider = serviceProvider;
            m_StringLocalizer = stringLocalizer;
        }

        public IEconomyDatabase DataBase { get; set; }

        protected override async Task OnLoadAsync()
        {
            if (!Enum.TryParse<StoreType>(Configuration["Store_Type"], out var storeType))
                throw new Exception(m_StringLocalizer["uconomy:fail:invalid_store_type", Configuration["Store_Type"],
                    string.Join(", ", Enum.GetNames(typeof(StoreType)))]);

            if (!decimal.TryParse(Configuration["Default_Balance"], out var defaultBalance) || defaultBalance < 0)
                throw new Exception(m_StringLocalizer["uconomy:fail:invalid_default_balance",
                    Configuration["Default_Balance"]]);

            if (!bool.TryParse(Configuration["Allow_Negative_Balance"], out var allowNegativeBalance))
                throw new Exception(m_StringLocalizer["uconomy:fail:invalid_negative_balance",
                    Configuration["Allow_Negative_Balance"]]);

            if (DataBase != null)
                await DataBase.DisposeAsync();

            DataBase = ActivatorUtilities.CreateInstance<DataBase.Database>(m_ServiceProvider, allowNegativeBalance,
                defaultBalance, storeType);
            await DataBase.LoadDatabaseAsync();
        }

        protected override async Task OnUnloadAsync()
        {
            await DataBase.DisposeAsync();
        }

        [EventListener]
        public Task OnUserConnectedAsync(object _, UserConnectedEvent userConnectedEvent)
        {
            return DataBase.CreateUserAccountAsync(userConnectedEvent.User.Id, userConnectedEvent.User.Type);
        }
    }
}