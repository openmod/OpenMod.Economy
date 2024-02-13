using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OpenMod.API.Ioc;

namespace OpenMod.Economy.API;

[Service]
public interface IEconomyDispatcher
{
    [UsedImplicitly]
    public Task EnqueueV2(Action action, Action<Exception>? exceptionHandler = null);

    public Task EnqueueV2(Func<Task> task, Action<Exception>? exceptionHandler = null);

    [UsedImplicitly]
    public Task<T> EnqueueV2<T>(Func<T> action, Action<Exception>? exceptionHandler = null);

    public Task<T> EnqueueV2<T>(Func<Task<T>> task, Action<Exception>? exceptionHandler = null);
}