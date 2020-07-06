#region

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace OpenMod.Economy.Helpers
{
    public abstract class ThreadHelper : IDisposable
    {
        private Thread m_CurrentLockedThread;
        private SemaphoreSlim m_SemaphoreLocker;

        protected ThreadHelper()
        {
            m_SemaphoreLocker = new SemaphoreSlim(1, 1);
        }

        public virtual void Dispose()
        {
            m_SemaphoreLocker?.Dispose();
            m_SemaphoreLocker = null;
        }

        public async Task ExecuteActionThreadSafeAsync(Func<Task> action)
        {
            bool isCurrentThread;
            lock (m_SemaphoreLocker)
            {
                isCurrentThread = m_CurrentLockedThread == Thread.CurrentThread;
            }

            if (isCurrentThread)
            {
                await action.Invoke();
                return;
            }

            try
            {
                await m_SemaphoreLocker.WaitAsync();
                m_CurrentLockedThread = Thread.CurrentThread;
                await action.Invoke();
            }
            finally
            {
                lock (m_SemaphoreLocker)
                {
                    m_CurrentLockedThread = null;
                }

                m_SemaphoreLocker.Release();
            }
        }

        public async Task<T> ExecuteActionThreadSafeAsync<T>(Func<Task<T>> action)
        {
            bool isCurrentThread;
            lock (m_SemaphoreLocker)
            {
                isCurrentThread = m_CurrentLockedThread == Thread.CurrentThread;
            }

            if (isCurrentThread) return await action.Invoke();

            try
            {
                await m_SemaphoreLocker.WaitAsync();
                m_CurrentLockedThread = Thread.CurrentThread;
                return await action.Invoke();
            }
            finally
            {
                lock (m_SemaphoreLocker)
                {
                    m_CurrentLockedThread = null;
                }

                m_SemaphoreLocker.Release();
            }
        }
    }
}