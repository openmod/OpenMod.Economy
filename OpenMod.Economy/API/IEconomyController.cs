#region

using System.Threading.Tasks;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.API
{
    public interface IEconomyController : IEconomyProvider
    {
        Task LoadDatabaseControllerAsync();
    }
}