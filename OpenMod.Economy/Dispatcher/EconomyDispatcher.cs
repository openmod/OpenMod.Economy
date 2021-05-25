#region

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Ioc;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.Dispatcher
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    [UsedImplicitly]
    public sealed class EconomyDispatcher : IEconomyDispatcher
    {
        private readonly ILogger<EconomyDispatcher> m_Logger;
        private readonly ConcurrentQueue<Func<Task>> m_QueueActions = new();

        private bool m_IsProcessing;

        public EconomyDispatcher(ILogger<EconomyDispatcher> logger)
        {
            m_Logger = logger;
        }

        private void ProcessQueue()
        {
            lock (this)
            {
                if (m_IsProcessing || m_QueueActions.IsEmpty)
                    return;

                m_IsProcessing = true;
            }

            Task.Run(async () =>
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

                    lock (this)
                    {
                        if (!m_QueueActions.IsEmpty)
                            continue;

                        m_IsProcessing = false;
                        return;
                    }
                } while (true);
            });
        }

        #region Enqueue

        public Task EnqueueV2(Action action, Action<Exception> exceptionHandler = null)
        {
            return EnqueueV2(() =>
            {
                action();
                return Task.FromResult(true);
            }, exceptionHandler);
        }

        public Task EnqueueV2(Func<Task> task, Action<Exception> exceptionHandler = null)
        {
            return EnqueueV2(() =>
            {
                task()?.GetAwaiter().GetResult();
                return Task.FromResult(true);
            }, exceptionHandler);
        }

        public Task<T> EnqueueV2<T>(Func<T> action, Action<Exception> exceptionHandler = null)
        {
            return EnqueueV2(() => Task.FromResult(action()), exceptionHandler);
        }

        public Task<T> EnqueueV2<T>(Func<Task<T>> task, Action<Exception> exceptionHandler = null)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
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
                    if (exceptionHandler == null)
                        throw;

                    exceptionHandler(ex);
                }
            });
            ProcessQueue();
            return tcs.Task;
        }

        #endregion
    }
}