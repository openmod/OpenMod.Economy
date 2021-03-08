#region

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal abstract class EconomyDatabaseCore : IEconomyProvider
    {
        protected readonly IConfiguration Configuration;

        protected EconomyDatabaseCore(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected string TableName => Configuration.GetSection("Database:Table_Name").Get<string>();
        protected decimal DefaultBalance => Configuration.GetSection("Economy:Default_Balance").Get<decimal>();
        public string CurrencyName => Configuration.GetSection("Economy:CurrencyName").Get<string>();
        public string CurrencySymbol => Configuration.GetSection("Economy:CurrencySymbol").Get<string>();

        public abstract Task<decimal> GetBalanceAsync(string ownerId, string ownerType);

        public abstract Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount,
            string reason);

        public abstract Task SetBalanceAsync(string ownerId, string ownerType, decimal balance);
    }
}