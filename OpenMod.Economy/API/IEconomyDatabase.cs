#region

using System;
using System.Threading.Tasks;

#endregion

namespace OpenMod.Economy.API
{
    public interface IEconomyDatabase : IAsyncDisposable // implement OpenMod.Extensions.Economy.Abstractions when ready
    {
        /// <summary>
        ///     Default balance used by the plugin, when creating a account
        /// </summary>
        decimal DefaultBalance { get; }

        /// <summary>
        ///     Called when economy loads, used to prepare database
        /// </summary>
        /// <returns>A Task that can be awaited until the action completes</returns>
        Task LoadDatabaseAsync();

        /// <summary>
        ///     Gets the balance
        /// </summary>
        /// <returns>Return the balance</returns>
        Task<decimal> GetBalanceAsync(IAccountId accountId);

        /// <summary>
        ///     Update the balance
        /// </summary>
        /// <returns>Return the balance</returns>
        Task<decimal> UpdateBalanceAsync(IAccountId accountId, decimal amount);

        /// <returns>Return the balance</returns>
        Task SetAccountAsync(IAccountId accountId, decimal balance);
    }
}