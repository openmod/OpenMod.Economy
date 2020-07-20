#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;
using OpenMod.Economy.API;

#endregion

[assembly: PluginMetadata("EconomyPlugin", Author = "OpenMod", DisplayName = "Economy")]

namespace OpenMod.Economy
{
    public sealed class Economy : OpenModUniversalPlugin
    {
        private readonly IEconomyDatabase m_EconomyDatabase;
        internal readonly IStringLocalizer StringLocalizer;

        public Economy(IEconomyDatabase economyDatabase, IServiceProvider serviceProvider,
            IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_EconomyDatabase = economyDatabase;
            StringLocalizer = stringLocalizer;
        }

        public StoreType DataStoreType { get; private set; }
        public decimal DefaultBalance { get; set; }

        protected override Task OnLoadAsync()
        {
            if (Enum.TryParse<StoreType>(Configuration["Store_Type"], out var storeType))
                DataStoreType = storeType;
            else
                throw new UserFriendlyException(StringLocalizer["economy:fail:invalid_store_type",
                    Configuration["Store_Type"],
                    string.Join(", ", Enum.GetNames(typeof(StoreType)))]);

            if (decimal.TryParse(Configuration["Default_Balance"], out var defaultBalance) && defaultBalance >= 0)
                DefaultBalance = defaultBalance;
            else
                throw new UserFriendlyException(StringLocalizer["economy:fail:invalid_default_balance",
                    Configuration["Default_Balance"]]);

            return m_EconomyDatabase.LoadDatabaseAsync();
        }
    }
}