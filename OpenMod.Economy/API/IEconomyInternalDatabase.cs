#region

using System.Threading.Tasks;

#endregion

namespace OpenMod.Economy.API
{
    internal interface IEconomyInternalDatabase
    {
        Task<decimal> GetBalanceAsync(string ownerId, string ownerType);

        Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount);

        Task SetAccountAsync(string ownerId, string ownerType, decimal balance);
    }
}