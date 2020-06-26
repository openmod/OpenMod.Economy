#region

using System;
using System.Threading.Tasks;

#endregion

namespace OpenMod.Economy.API
{
    public interface IEconomyDatabase : IAsyncDisposable
    {
        /// <summary>
        /// </summary>
        bool AllowNegativeBalance { get; }

        /// <summary>
        ///     Default balance used by the plugin, when creating a account
        /// </summary>
        decimal DefaultBalance { get; }

        /// <summary>
        ///     Called when uconomy loads, used to prepare database
        /// </summary>
        /// <returns>A Task that can be awaited until the action completes</returns>
        Task LoadDatabaseAsync();

        /// <summary>
        ///     Called when a player join, used to create a account for player
        /// </summary>
        /// <returns>Return if an account was created, false if already exists</returns>
        Task<bool> CreateUserAccountAsync(string userId, string userType);

        /// <summary>
        ///     Gets the balance of the player
        /// </summary>
        /// <returns>Return the balance</returns>
        Task<decimal> GetBalanceAsync(string userId, string userType);

        /// <summary>
        ///     Increase the player balance
        /// </summary>
        /// <returns>Return the balance</returns>
        Task<decimal> IncreaseBalanceAsync(string userId, string userType, decimal amount);

        /// <summary>
        ///     Decrease the player balance
        /// </summary>
        /// <returns>Return the balance</returns>
        Task<decimal> DecreaseBalanceAsync(string userId, string userType, decimal amount,
            bool? allowNegativeBalance = null);
    }
}