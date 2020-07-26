#region

using System;
using System.Collections.Concurrent;
using System.Threading;
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

        public void Enqueue(Action action)
        {
            s_QueueActions.Enqueue(action);
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
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogWarning(ex, "Exception in Dispatcher: ");
                    }
            }
        }
    }
}