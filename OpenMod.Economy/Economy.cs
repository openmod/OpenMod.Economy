#region

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;
using OpenMod.Economy.API;

#endregion

[assembly:
    PluginMetadata("OpenMod.Economy", Author = "OpenMod,Rube200", DisplayName = "Openmod.Economy",
        Website = "https://github.com/openmodplugins/OpenMod.Economy")]

namespace OpenMod.Economy
{
    public sealed class Economy : OpenModUniversalPlugin
    {
        internal readonly IStringLocalizer StringLocalizer;

        // ReSharper disable once SuggestBaseTypeForParameter
        public Economy(ILogger<Economy> logger, IServiceProvider serviceProvider, IStringLocalizer stringLocalizer) :
            base(serviceProvider)
        {
            StringLocalizer = stringLocalizer;
            var storeType = Configuration.GetSection("Database:Store_Type").Get<string>();
            var dbStoreType = Enum.TryParse<StoreType>(storeType, true, out var dbType) ? dbType : StoreType.DataStore;
            logger.LogInformation($"Database type set to: '{dbStoreType}'");
        }
    }
}