/*using System;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace OpenMod.Database.Helper
{
    public abstract class SqLiteHelper : ThreadHelper
    {
        protected SQLiteConnection SqLiteConnection;

        protected SqLiteHelper(string sqLiteString)
        {
            SqLiteConnection = new SQLiteConnection(sqLiteString);
        }

        public new virtual void Dispose()
        {
            SqLiteConnection?.Dispose();
            SqLiteConnection = null;
            base.Dispose();
        }


        public Task ExecuteSqLiteContextAsync(Func<SQLiteCommand, Task> action)
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                using var command = SqLiteConnection.CreateCommand();
                try
                {
                    await SqLiteConnection.OpenAsync();
                    await action.Invoke(command);
                }
                finally
                {
                    SqLiteConnection.Close();
                }
            });
        }

        public Task<T> ExecuteSqLiteContextAsync<T>(Func<SQLiteCommand, Task<T>> action)
        {
            return ExecuteActionThreadSafeAsync(async () =>
            {
                using var command = SqLiteConnection.CreateCommand();
                try
                {
                    await SqLiteConnection.OpenAsync();
                    return await action(command);
                }
                finally
                {
                    SqLiteConnection.Close();
                }
            });
        }
    }
}
*/

