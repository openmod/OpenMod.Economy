#region

using System;
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

        public async Task CheckShemasAsync()
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await using var command = connection.CreateCommand();
            await connection.OpenAsync();

            command.CommandText = $"SHOW TABLES LIKE '{TableName}';";
            if (await command.ExecuteScalarAsync() is not null)
                return;

            command.CommandText = $"CREATE TABLE `{TableName}` (" +
                                  "`Id` VARCHAR(255), " +
                                  "`Type` VARCHAR(20), " +
                                  "`Balance` DECIMAL NOT NULL, " +
                                  "PRIMARY KEY(`Id`, `Type`)) " +
                                  "COLLATE='utf8mb4_general_ci';";

            await command.ExecuteNonQueryAsync();
        }

        public override async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await using var command = connection.CreateCommand();

            command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
            command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
            command.CommandText = "SELECT `Balance` " +
                                  $"FROM `{TableName}` " +
                                  "WHERE `Id` = @ownerid AND `Type` = @ownertype;";

            await connection.OpenAsync();
            if (await command.ExecuteScalarAsync() is decimal balance) return balance;

            return DefaultBalance;
        }

        public override async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount,
            string _)
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await using var command = connection.CreateCommand();
            await connection.OpenAsync();

            command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
            command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
            command.Parameters.Add("@amount", MySqlDbType.Decimal).Value = amount;

            //Yet i know
            while (true)
            {
                command.CommandText = $"UPDATE `{TableName}` " + "SET `Balance` = `Balance` + @amount " +
                                      "WHERE `Id` = @ownerid AND `Type` = @ownertype;";

                if (await command.ExecuteNonQueryAsync() == 0)
                {
                    await CreateAccountIntenalAsync(ownerId, ownerType, DefaultBalance);
                    continue;
                }

                var balance = await GetBalanceAsync(ownerId, ownerType);
                if (balance >= 0 || amount >= 0) return balance;

                balance = await UpdateBalanceAsync(ownerId, ownerType, Math.Abs(amount), null);
                throw new NotEnoughBalanceException(
                    m_StringLocalizer["economy:fail:not_enough_balance",
                        new {Balance = balance, EconomyProvider = (IEconomyProvider) this}],
                    balance);
            }
        }

        public override async Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await using var command = connection.CreateCommand();
            await connection.OpenAsync();

            if (await CreateAccountIntenalAsync(ownerId, ownerType, balance))
                return;

            command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
            command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
            command.Parameters.Add("@balance", MySqlDbType.Decimal).Value = balance;
            command.CommandText = $"UPDATE `{TableName}` " +
                                  "SET `Balance` = @balance " +
                                  "WHERE `Id` = @ownerid AND `Type` = @ownertype;";

            await command.ExecuteNonQueryAsync();
        }

        private async Task<bool> CreateAccountIntenalAsync(string ownerId, string ownerType, decimal balance)
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await using var command = connection.CreateCommand();

            command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
            command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
            command.Parameters.Add("@balance", MySqlDbType.Decimal).Value = balance;
            command.CommandText = $"INSERT IGNORE INTO `{TableName}` " +
                                  "(`Id`, `Type`, `Balance`) VALUES " +
                                  "(@ownerid, @ownertype, @balance);";

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}