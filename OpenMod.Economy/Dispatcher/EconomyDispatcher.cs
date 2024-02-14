using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using OpenMod.API.Ioc;
using OpenMod.Core.Helpers;
using OpenMod.Economy.API;

namespace OpenMod.Economy.Dispatcher;

[ServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
[UsedImplicitly]
public sealed class EconomyDispatcher : IAsyncDisposable, IDisposable, IEconomyDispatcher
{
    private readonly ILogger<EconomyDispatcher> m_Logger;

    private readonly SemaphoreSlim m_Mutex = new(1);
    private readonly ConcurrentQueue<Func<Task>> m_QueueActions = new();

    private ManualResetEventSlim? m_DisposeWaitEvent = new();
    private DispatcherState m_State = DispatcherState.None;

    public EconomyDispatcher(ILogger<EconomyDispatcher> logger)
    {
        m_Logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        using (await m_Mutex.LockAsync())
        {
            if (m_State.HasFlag(DispatcherState.Disposed))
                return;

            m_State |= DispatcherState.Disposed;
            if (m_QueueActions.IsEmpty)
                return;

            m_DisposeWaitEvent = new ManualResetEventSlim();
        }

        try
        {
            m_DisposeWaitEvent?.Wait();
        }
        finally
        {
            m_DisposeWaitEvent?.Dispose();
            m_DisposeWaitEvent = null;
        }
    }

    public void Dispose()
    {
        AsyncContext.Run(DisposeAsync);
    }

    public Task EnqueueV2(Action action, Action<Exception>? exceptionHandler = null)
    {
        return EnqueueV2(() =>
        {
            action();
            return Task.FromResult(true);
        }, exceptionHandler);
    }

    public Task EnqueueV2(Func<Task> task, Action<Exception>? exceptionHandler = null)
    {
        return EnqueueV2(async () =>
        {
            await task();
            return Task.FromResult(true);
        }, exceptionHandler);
    }

    public Task<T> EnqueueV2<T>(Func<T> action, Action<Exception>? exceptionHandler = null)
    {
        return EnqueueV2(() => Task.FromResult(action()), exceptionHandler);
    }

    public Task<T> EnqueueV2<T>(Func<Task<T>> task, Action<Exception>? exceptionHandler = null)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncContext.Run(() => EnqueueInternal(task, tcs, exceptionHandler));
        return tcs.Task;
    }

    private async Task ProcessQueue()
    {
        do
        {
            while (m_QueueActions.TryDequeue(out var action))
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Exception while dispatching a task");
                }

            using (await m_Mutex.LockAsync())
            {
                if (!m_QueueActions.IsEmpty)
                    continue;

                m_State &= ~DispatcherState.Processing;
                m_DisposeWaitEvent?.Set();
                return;
            }
        } while (true);
    }

    private async Task EnqueueInternal<T>(Func<Task<T>> task, TaskCompletionSource<T> tcs,
        Action<Exception>? exceptionHandler = null)
    {
        using (await m_Mutex.LockAsync())
        {
            if (m_State.HasFlag(DispatcherState.Disposed))
            {
                tcs.SetCanceled();
                return;
            }

            m_QueueActions.Enqueue(async () =>
            {
                try
                {
                    var result = await task();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    if (exceptionHandler is null)
                    {
                        m_Logger.LogError(ex, "Fail to dispatch task");
                        return;
                    }

                    exceptionHandler(ex);
                }
            });

            if (m_State.HasFlag(DispatcherState.Processing))
                return;

            m_State |= DispatcherState.Processing;
            AsyncHelper.Schedule("Economy process queue", ProcessQueue, exceptionHandler);
        }
    }

    [Flags]
    private enum DispatcherState : byte
    {
        None = 0,
        Processing = 1,
        Disposed = 2
    }
}