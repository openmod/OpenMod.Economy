#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using MySqlConnector;
using OpenMod.Economy.API;
using OpenMod.Extensions.Economy.Abstractions;
using MySqlHelper = OpenMod.Economy.Helpers.MySqlHelper;

#endregion

namespace OpenMod.Economy.DataBase
{
    internal sealed class MySqlDatabase : MySqlHelper, IEconomyInternalDatabase
    {
        private readonly decimal m_DefaultBalance;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly string m_TableName;

        public MySqlDatabase(IConfiguration configuration, decimal defaultBalance, IStringLocalizer stringLocalizer,
            string tableName) : base(configuration["MySql_Connection_String"])
        {
            m_DefaultBalance = defaultBalance;
            m_StringLocalizer = stringLocalizer;
            m_TableName = tableName;
        }

        public Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            return ExecuteMySqlContextAsync(async command =>
            {
                command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
                command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
                command.CommandText = "SELECT `Balance` " +
                                      $"FROM `{m_TableName}` " +
                                      "WHERE `Id` = @ownerid AND `Type` = @ownertype;";

                if (await command.ExecuteScalarAsync() is decimal balance) return balance;

                await CreateAccountIntenalAsync(ownerId, ownerType, m_DefaultBalance);
                return m_DefaultBalance;
            });
        }

        public Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount)
        {
            return ExecuteMySqlContextAsync(async command =>
            {
                command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
                command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
                command.Parameters.Add("@amount", MySqlDbType.Decimal).Value = amount;
                command.CommandText = $"UPDATE `{m_TableName}` " +
                                      "SET `Balance` = `Balance` + @amount " +
                                      "WHERE `Id` = @ownerid AND `Type` = @ownertype;";

                if (await command.ExecuteNonQueryAsync() > 0)
                {
                    var balance = await GetBalanceAsync(ownerId, ownerType);
                    if (balance >= 0 || amount >= 0)
                        return balance;

                    balance = await UpdateBalanceAsync(ownerId, ownerType, Math.Abs(amount));
                    throw new NotEnoughBalanceException(m_StringLocalizer["economy:fail:not_enough_balance", balance]);
                }

                await CreateAccountIntenalAsync(ownerId, ownerType, m_DefaultBalance);
                return await UpdateBalanceAsync(ownerId, ownerType, amount);
            });
        }

        public async Task SetAccountAsync(string ownerId, string ownerType, decimal balance)
        {
            if (await CreateAccountIntenalAsync(ownerId, ownerType, balance))
                return;

            await ExecuteMySqlContextAsync(command =>
            {
                command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
                command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
                command.Parameters.Add("@balance", MySqlDbType.Decimal).Value = balance;
                command.CommandText = $"UPDATE `{m_TableName}` " +
                                      "SET `Balance` = @balance " +
                                      "WHERE `Id` = @ownerid AND `Type` = @ownertype;";

                return command.ExecuteNonQueryAsync();
            });
        }

        internal Task CheckShemasAsync()
        {
            return ExecuteMySqlContextAsync(async command =>
            {
                command.CommandText = $"SHOW TABLES LIKE '{m_TableName}';";
                if (await command.ExecuteScalarAsync() != null)
                    return;

                command.CommandText = $"CREATE TABLE `{m_TableName}` (" +
                                      "`Id` VARCHAR(255), " + //The size of 25 can be small, it work for unturned player_STEAMID or discord_STEAMID 
                                      "`Type` VARCHAR(20), " +
                                      "`Balance` DECIMAL NOT NULL, " +
                                      "PRIMARY KEY(`Id`, `Type`)) " +
                                      "COLLATE='utf8mb4_general_ci';";

                await command.ExecuteNonQueryAsync();
            });
        }

        private Task<bool> CreateAccountIntenalAsync(string ownerId, string ownerType, decimal balance)
        {
            return ExecuteMySqlContextAsync(async command =>
            {
                command.Parameters.Add("@ownerid", MySqlDbType.VarChar).Value = ownerId;
                command.Parameters.Add("@ownertype", MySqlDbType.VarChar).Value = ownerType;
                command.Parameters.Add("@balance", MySqlDbType.Decimal).Value = balance;
                command.CommandText = $"INSERT IGNORE INTO `{m_TableName}` " +
                                      "(`Id`, `Type`, `Balance`) VALUES " +
                                      "(@ownerid, @ownertype, @balance);";

                return await command.ExecuteNonQueryAsync() > 0;
            });
        }
    }
}