#region

using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.Controllers
{
    public abstract class DatabaseController
    {
        protected IConfiguration Configuration { get; private set; }
        protected IEventBus EventBus { get; private set; }
        protected ILifetimeScope LifetimeScope { get; private set; }
        protected IOpenModComponent OpenModComponent { get; private set; }
        protected IStringLocalizer StringLocalizer { get; private set; }

        public StoreType DbStoreType { get; private set; }

        protected abstract Task LoadControllerAsync();

        private async Task LoadControllerBaseAsync()
        {
            var storeType = Configuration.GetSection("Database:Store_Type").Get<string>();
            DbStoreType = Enum.TryParse<StoreType>(storeType, true, out var dbStoreType)
                ? dbStoreType
                : StoreType.DataStore;

            await LoadControllerAsync();
        }

        internal abstract Task ConfigurationChangedAsync();

        internal Task InjectAndLoad(ILifetimeScope lifetimeScope)
        {
            LifetimeScope = lifetimeScope;

            Configuration = lifetimeScope.Resolve<IConfiguration>();
            EventBus = lifetimeScope.Resolve<IEventBus>();
            OpenModComponent = lifetimeScope.Resolve<IOpenModComponent>();
            StringLocalizer = lifetimeScope.Resolve<IStringLocalizer>();

            return LoadControllerBaseAsync();
        }
    }
}