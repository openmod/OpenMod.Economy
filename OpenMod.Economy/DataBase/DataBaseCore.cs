#region

using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Plugins;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal abstract class DataBaseCore : IEconomyProvider
    {
        protected readonly IPluginAccessor<Economy> EconomyPlugin;

        protected DataBaseCore(IPluginAccessor<Economy> economyPlugin)
        {
            EconomyPlugin = economyPlugin;
        }

        protected decimal DefaultBalance => decimal.Parse(EconomyPlugin.Instance.Configuration["Default_Balance"]);
        protected IStringLocalizer StringLocalizer => EconomyPlugin.Instance.StringLocalizer;
        protected string TableName => EconomyPlugin.Instance.Configuration["Table_Name"];

        public abstract Task<decimal> GetBalanceAsync(string ownerId, string ownerType);
        public abstract Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount);
        public abstract Task SetBalanceAsync(string ownerId, string ownerType, decimal balance);
    }
}