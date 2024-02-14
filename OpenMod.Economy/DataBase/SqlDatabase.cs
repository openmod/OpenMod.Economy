using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OpenMod.Economy.DataBase;

public abstract class SqlDatabase<TConnection> : Database where TConnection : DbConnection
{
    protected SqlDatabase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override Task<bool> CheckSchemasAsync()
    {
        var token = GetCancellationToken();
        return RunQueryNonEnqueueAsync(async command =>
        {
            command.CommandText = $"SHOW TABLES LIKE '{TableName}';";
            if (await command.ExecuteScalarAsync(token) is not null)
                return true;

            command.CommandText = $"CREATE TABLE `{TableName}` (" +
                                  "`Id` VARCHAR(128), " +
                                  "`Type` VARCHAR(20), " +
                                  "`Balance` DECIMAL(10,2) NOT NULL, " +
                                  "PRIMARY KEY(`Id`, `Type`)) " +
                                  "COLLATE='utf8mb4_general_ci';";
            await command.ExecuteNonQueryAsync(token);
            return false;
        });
    }

    private async Task<bool> CreateAccountIntenalAsync(DbCommand command, decimal? balance = null)
    {
        GetParameter(command, "@balance", DbType.Decimal, balance ?? DefaultBalance);
        command.CommandText = $"INSERT IGNORE INTO `{TableName}` " +
                              $"(`Id`, `Type`, `Balance`) " +
                              $"VALUES " +
                              $"(@ownerid, @ownertype, @balance);";
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
    {
        var token = GetCancellationToken();
        return RunQueryAsync(async command =>
        {
            GetParameter(command, "@ownerid", DbType.String, ownerId);
            GetParameter(command, "@ownertype", DbType.String, ownerType);
            if (await CreateAccountIntenalAsync(command))
                return DefaultBalance;


            command.CommandText = "SELECT `Balance` " +
                                  $"FROM `{TableName}` " +
                                  "WHERE `Id`=@ownerid " +
                                  "AND `Type`=@ownertype;";
            return await command.ExecuteScalarAsync(token) as decimal? ?? DefaultBalance;
        });
    }

    public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
    {
        var token = GetCancellationToken();
        return RunQueryAsync(async command =>
        {
            GetParameter(command, "@ownerid", DbType.String, ownerId);
            GetParameter(command, "@ownertype", DbType.String, ownerType);
            if (await CreateAccountIntenalAsync(command, balance))
                return;

            command.Parameters["@balance"].Value = balance;
            command.CommandText = $"UPDATE `{TableName}` " +
                                  "SET `Balance`=@balance " +
                                  "WHERE `Id`=@ownerid " +
                                  "AND `Type`=@ownertype;";
            await command.ExecuteNonQueryAsync(token);
        });
    }

    public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string? _)
    {
        var token = GetCancellationToken();
        return RunQueryAsync(async command =>
        {
            GetParameter(command, "@ownerid", DbType.String, ownerId);
            GetParameter(command, "@ownertype", DbType.String, ownerType);
            GetParameter(command, "@amount", DbType.Decimal, amount);

            var newBalance = DefaultBalance + amount;
            if (await CreateAccountIntenalAsync(command, newBalance))
                return newBalance;

            command.CommandText = $"UPDATE `{TableName}` " +
                                  "SET `Balance`=`Balance`+@amount " +
                                  "WHERE `Id`=@ownerid " +
                                  "AND `Type`=@ownertype " +
                                  "AND `Balance`+@amount>='0';";

            var success = await command.ExecuteNonQueryAsync(token) > 0;
            var balance = await GetBalanceAsync(ownerId, ownerType);
            if (success)
                return balance;

            throw ThrowNotEnoughtBalance(amount, balance);
        });
    }

    private async Task RunConnectionAsync(Func<DbConnection, Task> action)
    {
        var token = CancellationToken.None;
#if NETSTANDARD2_1_OR_GREATER
        await using var connection = (TConnection)Activator.CreateInstance(typeof(TConnection), ConnectionString);
#else
        using var connection = (TConnection)Activator.CreateInstance(typeof(TConnection), ConnectionString);
#endif
        await connection.OpenAsync(token);
        await action(connection);
    }

    private async Task<TReturn> RunConnectionAsync<TReturn>(Func<DbConnection, Task<TReturn>> action)
    {
        var token = CancellationToken.None;
#if NETSTANDARD2_1_OR_GREATER
        await using var connection = (TConnection)Activator.CreateInstance(typeof(TConnection), ConnectionString);
#else
        using var connection = (TConnection)Activator.CreateInstance(typeof(TConnection), ConnectionString);
#endif
        await connection.OpenAsync(token);
        return await action(connection);
    }

    private static async Task<TReturn> RunCommandAsync<TReturn>(DbConnection connection,
        Func<DbCommand, Task<TReturn>> action)
    {
#if NETSTANDARD2_1_OR_GREATER
        await using var command = connection.CreateCommand();
#else
        using var command = connection.CreateCommand();
#endif
        return await action(command);
    }

    private static async Task RunCommandAsync(DbConnection connection, Func<DbCommand, Task> action)
    {
#if NETSTANDARD2_1_OR_GREATER
        await using var command = connection.CreateCommand();
#else
        using var command = connection.CreateCommand();
#endif
        await action(command);
    }

    private Task<TReturn> RunQueryNonEnqueueAsync<TReturn>(Func<DbCommand, Task<TReturn>> action)
    {
        return RunConnectionAsync(connection => RunCommandAsync(connection, action));
    }

    private Task RunQueryAsync(Func<DbCommand, Task> action)
    {
        return Enqueue(() => RunConnectionAsync(connection => RunCommandAsync(connection, action)));
    }

    private Task<TReturn> RunQueryAsync<TReturn>(Func<DbCommand, Task<TReturn>> action)
    {
        return Enqueue(() => RunConnectionAsync(connection => RunCommandAsync(connection, action)));
    }

    private static void GetParameter(DbCommand command, string parameterName, DbType parameterType, object value)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = parameterName;
        parameter.DbType = parameterType;
        parameter.Value = value;

        command.Parameters.Add(parameter);
    }
}