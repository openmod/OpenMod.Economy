#region

using System.Threading.Tasks;

#endregion

namespace OpenMod.Economy.API
{
    internal interface IEconomyInternalDatabase
    {
        Task<decimal> GetBalanceAsync(IAccountId accountId);

        Task<decimal> UpdateBalanceAsync(IAccountId accountId, decimal amount);

        Task SetAccountAsync(IAccountId accountId, decimal balance);
    }
}