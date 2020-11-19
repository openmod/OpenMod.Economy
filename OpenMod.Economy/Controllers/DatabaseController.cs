#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Plugins;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.Controllers
{
    ///Do not forget to call 'LoadControllerBaseAsync'
    public abstract class DatabaseController
    {
        protected readonly IPluginAccessor<Economy> EconomyPlugin;

        protected DatabaseController(IPluginAccessor<Economy> economyPlugin)
        {
            EconomyPlugin = economyPlugin;
        }

        protected StoreType DbStoreType { get; private set; }
        protected bool IsServiceLoaded { get; private set; }

        protected IConfiguration Configuration => EconomyPlugin.Instance.Configuration;
        protected IStringLocalizer StringLocalizer => EconomyPlugin.Instance.StringLocalizer;

        protected async Task LoadControllerBaseAsync()
        {
            if (IsServiceLoaded || EconomyPlugin.Instance == null)
                return;

            var storeType = Configuration.GetSection("Database:Store_Type").Get<string>();
            DbStoreType = Enum.TryParse<StoreType>(storeType, out var dbStoreType) ? dbStoreType : StoreType.DataStore;

            await LoadControllerAsync();
            IsServiceLoaded = true;
        }

        /// Never call this method, you should call 'LoadControllerBaseAsync' instead.
        protected abstract Task LoadControllerAsync();
    }
}