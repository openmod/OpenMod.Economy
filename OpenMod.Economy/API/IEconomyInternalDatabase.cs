#region

using System.Threading.Tasks;

#endregion

namespace OpenMod.Economy.API
{
    internal interface IEconomyInternalDatabase
    {
        Task<bool> CreateUserAccountAsync(string userId, string userType);

        Task<decimal> GetBalanceAsync(string userId, string userType);

        Task<decimal> IncreaseBalanceAsync(string userId, string userType, decimal amount);

        Task<decimal> DecreaseBalanceAsync(string userId, string userType, decimal amount,
            bool allowNegativeBalance);
    }
}