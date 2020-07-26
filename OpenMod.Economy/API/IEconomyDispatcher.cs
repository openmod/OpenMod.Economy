#region

using System;
using System.Threading.Tasks;
using OpenMod.API.Ioc;

#endregion

namespace OpenMod.Economy.API
{
    [Service]
    public interface IEconomyDispatcher
    {
        void Enqueue(Func<Task> action, Action<Exception> exceptionHandler = null);
        void LoadDispatcher();
    }
}