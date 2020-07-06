#region

using System;
using System.Threading.Tasks;
using LiteDB;

#endregion

namespace OpenMod.Economy.Helpers
{
    public abstract class LiteDbHelper : ThreadHelper
    {
        private readonly ConnectionString m_LiteDbConnection;

        protected LiteDbHelper(string liteDbString)
        {
            m_LiteDbConnection = new ConnectionString(liteDbString);
        }

        public Task ExecuteLiteDbContextAsync(Action<LiteDatabase> action)
        {
            return ExecuteActionThreadSafeAsync(() =>
            {
                using var dataBase = new LiteDatabase(m_LiteDbConnection);
                action.Invoke(dataBase);
                return Task.CompletedTask;
            });
        }

        public Task<T> ExecuteLiteDbContextAsync<T>(Func<LiteDatabase, T> action)
        {
            return ExecuteActionThreadSafeAsync(() =>
            {
                using var dataBase = new LiteDatabase(m_LiteDbConnection);
                return Task.FromResult(action(dataBase));
            });
        }
    }
}