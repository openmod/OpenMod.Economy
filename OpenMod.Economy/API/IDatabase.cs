using System.Threading.Tasks;
using OpenMod.Extensions.Economy.Abstractions;

namespace OpenMod.Economy.API;

public interface IDatabase : IEconomyProvider
{
    /// <summary>
    ///     Checks and creates table/collection if it doesnt exists
    /// </summary>
    /// <returns>true is exists, false if creates it</returns>
    Task<bool> CheckSchemasAsync();
}