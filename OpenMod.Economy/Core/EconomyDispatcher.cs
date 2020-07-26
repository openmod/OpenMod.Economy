#region

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Ioc;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.Core
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public sealed class EconomyDispatcher : IEconomyDispatcher, IDisposable
    {
        private static readonly ConcurrentQueue<Action> s_QueueActions = new ConcurrentQueue<Action>();
        private static readonly AutoResetEvent s_WaitHandle = new AutoResetEvent(false);
        private readonly ILogger<Economy> m_Logger;

        private bool m_Disposed;

        public EconomyDispatcher(ILogger<Economy> logger)
        {
            m_Logger = logger;
        }

        public void Dispose()
        {
            m_Disposed = true;
        }

        public void Enqueue(Func<Task> task, Action<Exception> exceptionHandler = null)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            s_QueueActions.Enqueue(async () =>
            {
                try
                {
                    await task();
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                    {
                        exceptionHandler(ex);
                    }
                    else
                    {
                        m_Logger.LogError(ex, "Exception while dispatching a task");
                    }
                }
            });
            s_WaitHandle.Set();
        }

        public void LoadDispatcher()
        {
            new Thread(Looper).Start();
        }

        private void Looper()
        {
            while (!m_Disposed)
            {
                s_WaitHandle.WaitOne();
                while (s_QueueActions.TryDequeue(out var action))
                {
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
}