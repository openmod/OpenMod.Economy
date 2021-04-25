#region

using System;
using System.Collections.Concurrent;
using System.Threading;
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
    public sealed class EconomyDispatcher : IEconomyDispatcher, IDisposable
    {
        private readonly ILogger<Economy> m_Logger;
        private readonly ConcurrentQueue<Action> m_QueueActions = new();
        private readonly AutoResetEvent m_WaitHandle = new(false);

        private bool m_Disposed;
        private Thread m_LoopThread;

        public EconomyDispatcher(ILogger<Economy> logger)
        {
            m_Logger = logger;
        }

        public void Dispose()
        {
            lock (this)
            {
                if (m_Disposed)
                    return;

                m_Disposed = true;
            }

            m_WaitHandle.Set();

            m_LoopThread?.Join();
            m_LoopThread = null;

            m_WaitHandle?.Dispose();
        }

        private bool LoadDispatcher()
        {
            lock (this)
            {
                if (m_Disposed)
                    return false;

                if (m_LoopThread is not null)
                    return true;

                m_LoopThread = new Thread(Looper);
            }

            m_LoopThread.Start();
            return true;
        }

        private void Looper()
        {
            while (true)
            {
                lock (this)
                {
                    if (m_Disposed)
                        return;
                }

                m_WaitHandle.WaitOne();
                ProcessQueue();
            }
        }

        private void ProcessQueue()
        {
            while (m_QueueActions.TryDequeue(out var action))
                //Try catch prevents exception in case of direct insert on ConcurrentQueue instead of Enqueue it
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Exception while dispatching a task");
                }
        }

        #region Enqueue

        public Task EnqueueV2(Action action, Action<Exception> exceptionHandler = null)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            if (!LoadDispatcher())
                throw new ObjectDisposedException(nameof(EconomyDispatcher));

            var tcs = new TaskCompletionSource<Task>();
            m_QueueActions.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(Task.CompletedTask);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    exceptionHandler?.Invoke(ex);
                }
            });
            m_WaitHandle.Set();
            return tcs.Task;
        }

        public Task EnqueueV2(Func<Task> task, Action<Exception> exceptionHandler = null)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            if (!LoadDispatcher())
                throw new ObjectDisposedException(nameof(EconomyDispatcher));

            var tcs = new TaskCompletionSource<Task>();
            m_QueueActions.Enqueue(() =>
            {
                try
                {
                    tcs.SetResult(task());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    exceptionHandler?.Invoke(ex);
                }
            });
            m_WaitHandle.Set();
            return tcs.Task;
        }

        public Task<T> EnqueueV2<T>(Func<T> action, Action<Exception> exceptionHandler = null)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            if (!LoadDispatcher())
                throw new ObjectDisposedException(nameof(EconomyDispatcher));

            var tcs = new TaskCompletionSource<T>();
            m_QueueActions.Enqueue(() =>
            {
                try
                {
                    tcs.SetResult(action());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    exceptionHandler?.Invoke(ex);
                }
            });
            m_WaitHandle.Set();
            return tcs.Task;
        }

        public Task<T> EnqueueV2<T>(Func<Task<T>> task, Action<Exception> exceptionHandler = null)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            if (!LoadDispatcher())
                throw new ObjectDisposedException(nameof(EconomyDispatcher));

            var tcs = new TaskCompletionSource<T>();
            m_QueueActions.Enqueue(async () =>
            {
                try
                {
                    tcs.SetResult(await task());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    exceptionHandler?.Invoke(ex);
                }
            });
            m_WaitHandle.Set();
            return tcs.Task;
        }

        #endregion
    }
}