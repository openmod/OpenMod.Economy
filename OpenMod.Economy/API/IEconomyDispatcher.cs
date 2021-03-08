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
        public Task EnqueueV2(Action action, Action<Exception> exceptionHandler = null);
        public Task EnqueueV2(Func<Task> task, Action<Exception> exceptionHandler = null);

        public Task<T> EnqueueV2<T>(Func<T> action, Action<Exception> exceptionHandler = null);
        public Task<T> EnqueueV2<T>(Func<Task<T>> task, Action<Exception> exceptionHandler = null);
    }
}