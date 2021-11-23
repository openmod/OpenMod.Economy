#region

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using MySqlConnector;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class MySqlDatabase : EconomyDatabaseCore
    {
        private readonly IStringLocalizer m_StringLocalizer;

        public MySqlDatabase(IConfiguration configuration, IStringLocalizer stringLocalizer) : base(configuration)
        {
            m_StringLocalizer = stringLocalizer;
        }

        private string ConnectionString => Configuration
            .GetSection("Database:Connection_String")
            .Get<string>();

        public Task CheckShemasAsync()
        {
            return ExecuteMySqlAsync(async command =>
            {
                command.CommandText = $"SHOW TABLES LIKE '{TableName}';";
                if (await command.ExecuteScalarAsync() != null)
                    return;

                command.CommandText = $"CREATE TABLE `{TableName}` (" +
                                      "`Id` VARCHAR(128), " +
                                      "`Type` VARCHAR(20), " +
                                      "`Balance` DECIMAL(10,2) NOT NULL, " +
                                      "PRIMARY KEY(`Id`, `Type`)) " +
                                      "COLLATE='utf8mb4_general_ci';";
                await command.ExecuteNonQueryAsync();
            });
        }

        private async Task<bool> CreateAccountIntenalAsync(DbCommand command)
        {
            command.CommandText = $"INSERT IGNORE INTO `{TableName}` " +
                                  "(`Id`, `Type`, `Balance`) VALUES " +
                                  "(@ownerid, @ownertype, @balance);";
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            return ExecuteMySqlAsync(async command =>
            {
                command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
                command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
                command.Parameters.Add("@balance", MySqlDbType.Decimal).Value = DefaultBalance;
                if (await CreateAccountIntenalAsync(command))
                    return DefaultBalance;


                command.CommandText = "SELECT `Balance` " +
                                      $"FROM `{TableName}` " +
                                      "WHERE `Id`=@ownerid AND `Type`=@ownertype;";
                return await command.ExecuteScalarAsync() as decimal? ?? DefaultBalance;
            });
        }

        public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            return ExecuteMySqlAsync(async command =>
            {
                command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
                command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
                command.Parameters.Add("@balance", MySqlDbType.Decimal).Value = balance;
                if (await CreateAccountIntenalAsync(command))
                    return;

                command.CommandText = $"UPDATE `{TableName}` " +
                                      "SET `Balance`=@balance " +
                                      "WHERE `Id`=@ownerid AND `Type`=@ownertype;";
                await command.ExecuteNonQueryAsync();
            });
        }

        public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount,
            string _)
        {
            return ExecuteMySqlAsync(async command =>
            {
                command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
                command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
                command.Parameters.Add("@amount", MySqlDbType.Decimal).Value = amount;

                var newDefault = DefaultBalance + amount;
                command.Parameters.Add("@balance", MySqlDbType.Decimal).Value = newDefault;

                if (await CreateAccountIntenalAsync(command))
                    return newDefault;

                command.CommandText = $"UPDATE `{TableName}` " +
                                      "SET `Balance`=`Balance`+@amount " +
                                      "WHERE `Id`=@ownerid " +
                                      "AND `Type`=@ownertype" +
                                      $"{(amount < 0 ? " AND `Balance`+@amount>='0'" : string.Empty)};";

                var success = await command.ExecuteNonQueryAsync() > 0;
                var balance = await GetBalanceAsync(ownerId, ownerType);
                if (success)
                    return balance;

                throw new NotEnoughBalanceException(
                    m_StringLocalizer["economy:fail:not_enough_balance",
                        new {Amount = amount, Balance = balance, EconomyProvider = (IEconomyProvider) this}],
                    balance);
            });
        }

        private async Task<T> ExecuteMySqlAsync<T>(Func<MySqlCommand, Task<T>> task)
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await using var command = connection.CreateCommand();
            await connection.OpenAsync();
            return await task(command);
        }

        private async Task ExecuteMySqlAsync(Func<MySqlCommand, Task> task)
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await using var command = connection.CreateCommand();
            await connection.OpenAsync();
            await task(command);
        }
    }
}