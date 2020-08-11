#region

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Plugins;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal abstract class EconomyDatabaseCore : IEconomyProvider
    {
        protected readonly IPluginAccessor<Economy> EconomyPlugin;

        protected EconomyDatabaseCore(IPluginAccessor<Economy> economyPlugin)
        {
            EconomyPlugin = economyPlugin;
        }

        protected IStringLocalizer StringLocalizer => EconomyPlugin.Instance.StringLocalizer;

        protected string TableName =>
            EconomyPlugin.Instance.Configuration.GetSection("Database:Table_Name").Get<string>();

        protected decimal DefaultBalance =>
            EconomyPlugin.Instance.Configuration.GetSection("Default_Balance").Get<decimal>();

        public string CurrencyName =>
            EconomyPlugin.Instance.Configuration.GetSection("Economy:CurrencyName").Get<string>();

        public string CurrencySymbol =>
            EconomyPlugin.Instance.Configuration.GetSection("Economy:CurrencySymbol").Get<string>();

        public abstract Task<decimal> GetBalanceAsync(string ownerId, string ownerType);

        public abstract Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount,
            string reason);

        public abstract Task SetBalanceAsync(string ownerId, string ownerType, decimal balance);
    }
}