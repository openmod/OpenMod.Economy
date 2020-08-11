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
        private readonly ConcurrentQueue<Action> m_QueueActions = new ConcurrentQueue<Action>();
        private readonly AutoResetEvent m_WaitHandle = new AutoResetEvent(false);

        private bool m_Disposed;
        private bool m_IsLoaded;

        public EconomyDispatcher(ILogger<Economy> logger)
        {
            m_Logger = logger;
        }

        public void Dispose()
        {
            m_IsLoaded = false;
            m_Disposed = true;
        }

        public void Enqueue(Func<Task> task, Action<Exception> exceptionHandler = null)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            LoadDispatcher();
            m_QueueActions.Enqueue(async () =>
            {
                try
                {
                    await task();
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                        exceptionHandler(ex);
                    else
                        m_Logger.LogError(ex, "Exception while dispatching a task");
                }
            });
            m_WaitHandle.Set();
        }

        private void LoadDispatcher()
        {
            lock (this)
            {
                if (m_IsLoaded)
                    return;

                m_IsLoaded = true;
            }

            new Thread(Looper).Start();
        }

        private void Looper()
        {
            while (!m_Disposed)
            {
                m_WaitHandle.WaitOne();
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
        }
    }
}