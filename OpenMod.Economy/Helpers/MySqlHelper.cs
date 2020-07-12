#region

using System;
using System.Threading.Tasks;
using MySqlConnector;

#endregion

namespace OpenMod.Economy.Helpers
{
    public abstract class MySqlHelper : ThreadHelper
    {
        private readonly string m_MySqlConnectionString;
        private volatile ushort m_Threads;

        protected MySqlHelper(string mySqlString)
        {
            m_MySqlConnectionString = mySqlString;
        }

        public Task ExecuteMySqlContextAsync(Func<MySqlCommand, Task> action)
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                await using var connection = new MySqlConnection(m_MySqlConnectionString);
                await using var command = connection.CreateCommand();
                try
                {
                    m_Threads++;
                    if (m_Threads == 1)
                        await connection.OpenAsync();

                    await action.Invoke(command);
                }
                finally
                {
                    m_Threads--;
                    if (m_Threads == 0)
                        await connection.CloseAsync();
                }
            });
        }

        public Task<T> ExecuteMySqlContextAsync<T>(Func<MySqlCommand, Task<T>> action)
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                await using var connection = new MySqlConnection(m_MySqlConnectionString);
                await using var command = connection.CreateCommand();
                try
                {
                    m_Threads++;
                    if (m_Threads == 1)
                        await connection.OpenAsync();

                    return await action(command);
                }
                finally
                {
                    m_Threads--;
                    if (m_Threads == 0)
                        await connection.CloseAsync();
                }
            });
        }
    }
}