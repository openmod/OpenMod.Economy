#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.Controllers
{
    ///Do not forget to call 'LoadControllerBaseAsync'
    public abstract class DatabaseController
    {
        protected readonly IConfiguration Configuration;
        protected readonly IStringLocalizer StringLocalizer;

        protected DatabaseController(IConfiguration configuration, IStringLocalizer stringLocalizer)
        {
            Configuration = configuration;
            StringLocalizer = stringLocalizer;
        }

        public StoreType DbStoreType { get; private set; }
        public bool IsServiceLoaded { get; private set; }

        /// Never call this method, you should call 'LoadControllerBaseAsync' instead.
        protected abstract Task LoadControllerAsync();

        protected async Task LoadControllerBaseAsync()
        {
            if (IsServiceLoaded)
                return;

            var storeType = Configuration.GetSection("Database:Store_Type").Get<string>();
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
            return IsServiceLoaded ? ConfigurationChangedAsync() : Task.CompletedTask;
        }
    }
}