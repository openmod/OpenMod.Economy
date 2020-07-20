#region

using System.Threading.Tasks;
using OpenMod.API.Ioc;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.API
{
    [Service]
    public interface IEconomyDatabase : IEconomyProvider
    {
        /// <summary>
        ///     Called when economy loads, used to prepare database
        /// </summary>
        /// <returns>A Task that can be awaited until the action completes</returns>
        Task LoadDatabaseAsync();
    }
}