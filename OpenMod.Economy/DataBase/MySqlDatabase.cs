using System;
using MySqlConnector;

namespace OpenMod.Economy.DataBase;

internal sealed class MySqlDatabase(IServiceProvider serviceProvider) : SqlDatabase<MySqlConnection>(serviceProvider);