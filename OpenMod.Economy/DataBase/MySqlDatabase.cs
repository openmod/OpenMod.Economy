using System;
using MySqlConnector;

namespace OpenMod.Economy.DataBase;

internal sealed class MySqlDatabase : SqlDatabase<MySqlConnection>
{
    public MySqlDatabase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}