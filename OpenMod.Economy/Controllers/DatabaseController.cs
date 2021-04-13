#region

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Plugins;
using OpenMod.Economy.API;
using System;
using System.Threading.Tasks;

#endregion

namespace OpenMod.Economy.Controllers
{
    ///Do not forget to call 'LoadControllerBaseAsync'
    public abstract class DatabaseController
    {
        protected readonly IPluginAccessor<Economy> Plugin;
        private IStringLocalizer m_CachedStringLocalizer;
        private IConfiguration m_CachedConfiguration;

        protected DatabaseController(IPluginAccessor<Economy> plugin)
        {
            Plugin = plugin;
        }

        protected IStringLocalizer StringLocalizer
        {
            get
            {
                if (Plugin.Instance is null)
                    return m_CachedStringLocalizer;

                m_CachedStringLocalizer ??= Plugin.Instance.StringLocalizer;
                return Plugin.Instance.StringLocalizer;
            }
        }

        protected IConfiguration Configuration
        {
            get
            {
                if (Plugin.Instance is null)
                    return m_CachedConfiguration;

                m_CachedConfiguration ??= Plugin.Instance.Configuration;
                return Plugin.Instance.Configuration;
            }
        }

        public StoreType DbStoreType { get; private set; }
        public bool IsServiceLoaded { get; private set; }

        /// Never call this method, you should call 'LoadControllerBaseAsync' instead.
        protected abstract Task LoadControllerAsync();

        protected async Task LoadControllerBaseAsync()
        {
            if (IsServiceLoaded)
                return;

            // ReSharper disable once PossibleNullReferenceException
            var storeType = Plugin.Instance.Configuration.GetSection("Database:Store_Type").Get<string>();
            DbStoreType = Enum.TryParse<StoreType>(storeType, true, out var dbStoreType)
                ? dbStoreType
                : StoreType.DataStore;

            await LoadControllerAsync();
            IsServiceLoaded = true;
        }

        /// Never call this method, you should call 'ConfigurationChangedBaseAsync' instead.
        protected abstract Task ConfigurationChangedAsync();

        /// This method should be called when IConfiguration change
        internal Task ConfigurationChangedBaseAsync()
        {
            m_CachedStringLocalizer = null;
            m_CachedConfiguration = null;
            return IsServiceLoaded ? ConfigurationChangedAsync() : Task.CompletedTask;
        }
    }
}