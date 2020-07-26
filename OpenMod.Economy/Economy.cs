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
        internal readonly IStringLocalizer StringLocalizer;

        public Economy(IServiceProvider serviceProvider, IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            StringLocalizer = stringLocalizer;
        }

        protected override Task OnLoadAsync()
        {
            if (!Enum.TryParse<StoreType>(Configuration["Store_Type"], true, out _))
                throw new UserFriendlyException(StringLocalizer["economy:fail:invalid_store_type",
                    new
                    {
                        storeType = Configuration["Store_Type"],
                        storeTypes = string.Join(", ", Enum.GetNames(typeof(StoreType)))
                    }]);

            if (!decimal.TryParse(Configuration["Default_Balance"], out var defaultBalance) || defaultBalance < 0)
                throw new UserFriendlyException(StringLocalizer["economy:fail:invalid_default_balance",
                    new {balance = Configuration["Default_Balance"]}]);

            return Task.CompletedTask;
        }
    }
}