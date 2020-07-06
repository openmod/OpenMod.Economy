#region

using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

#endregion

namespace OpenMod.Economy.Helpers
{
    public abstract class MySqlHelper : ThreadHelper
    {
        private MySqlConnection m_MySqlConnection;
        private volatile ushort m_Threads;

        protected MySqlHelper(string mySqlString)
        {
            m_MySqlConnection = new MySqlConnection(mySqlString);
        }

        public new virtual void Dispose()
        {
            m_MySqlConnection?.Dispose();
            m_MySqlConnection = null;
            base.Dispose();
        }

        public Task ExecuteMySqlContextAsync(Func<MySqlCommand, Task> action)
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                await using var command = m_MySqlConnection.CreateCommand();
                try
                {
                    m_Threads++;
                    if (m_Threads == 1)
                        await m_MySqlConnection.OpenAsync();

                    await action.Invoke(command);
                }
                finally
                {
                    m_Threads--;
                    if (m_Threads == 0)
                        await m_MySqlConnection.CloseAsync();
                }
            });
        }

        public Task<T> ExecuteMySqlContextAsync<T>(Func<MySqlCommand, Task<T>> action)
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                await using var command = m_MySqlConnection.CreateCommand();
                try
                {
                    m_Threads++;
                    if (m_Threads == 1)
                        await m_MySqlConnection.OpenAsync();

                    return await action(command);
                }
                finally
                {
                    m_Threads--;
                    if (m_Threads == 0)
                        await m_MySqlConnection.CloseAsync();
                }
            });
        }
    }
}