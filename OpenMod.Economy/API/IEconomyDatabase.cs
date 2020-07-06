#region

using System;
using System.Threading.Tasks;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.API
{
    public interface IEconomyDatabase : IEconomyProvider, IAsyncDisposable
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
    }
}