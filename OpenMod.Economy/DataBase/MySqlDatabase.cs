#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using MySql.Data.MySqlClient;
using OpenMod.Core.Commands;
using OpenMod.Economy.API;
using MySqlHelper = OpenMod.Database.Helper.MySqlHelper;

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

        public Task<decimal> GetBalanceAsync(IAccountId accountId)
        {
            var uniqueId = $"{accountId.OwnerType}_{accountId.OwnerId}";
            return ExecuteMySqlContextAsync(async command =>
            {
                command.Parameters.Add("@uniqueId", MySqlDbType.VarChar).Value = uniqueId;
                command.CommandText = "SELECT `Balance` " +
                                      $"FROM `{m_TableName}` " +
                                      "WHERE `UniqueId`=@uniqueId;";

                if (await command.ExecuteScalarAsync() is decimal balance) return balance;

                await CreateAccountIntenalAsync(uniqueId, m_DefaultBalance);
                return m_DefaultBalance;
            });
        }

        public Task<decimal> UpdateBalanceAsync(IAccountId accountId, decimal amount)
        {
            var uniqueId = $"{accountId.OwnerType}_{accountId.OwnerId}";
            return ExecuteMySqlContextAsync(async command =>
            {
                command.Parameters.Add("@uniqueId", MySqlDbType.VarChar).Value = uniqueId;
                command.Parameters.Add("@amount", MySqlDbType.Decimal).Value = amount;
                command.CommandText = $"UPDATE `{m_TableName}` " +
                                      "SET `Balance` = `Balance` + @amount " +
                                      "WHERE `UniqueId` = @uniqueId;";

                if (await command.ExecuteNonQueryAsync() > 0)
                {
                    var balance = await GetBalanceAsync(accountId);
                    if (balance >= 0 || amount >= 0)
                        return balance;

                    balance = await UpdateBalanceAsync(accountId, Math.Abs(amount));
                    throw new UserFriendlyException(m_StringLocalizer["economy:fail:not_enough_balance", balance]);
                }

                await CreateAccountIntenalAsync(uniqueId, m_DefaultBalance);
                return await UpdateBalanceAsync(accountId, amount);
            });
        }

        public async Task SetAccountAsync(IAccountId accountId, decimal balance)
        {
            var uniqueId = $"{accountId.OwnerType}_{accountId.OwnerId}";
            if (await CreateAccountIntenalAsync(uniqueId, balance))
                return;

            await ExecuteMySqlContextAsync(command =>
            {
                command.Parameters.Add("@uniqueId", MySqlDbType.VarChar).Value = uniqueId;
                command.Parameters.Add("@balance", MySqlDbType.Decimal).Value = balance;
                command.CommandText = $"UPDATE `{m_TableName}` " +
                                      "SET `Balance` = @balance " +
                                      "WHERE `UniqueId` = @uniqueId;";

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
                                      "`UniqueId` VARCHAR(25), " + //The size of 25 can be small, it work for unturned player_STEAMID or discord_STEAMID 
                                      "`Balance` DECIMAL NOT NULL, " +
                                      "PRIMARY KEY(`UniqueId`)) " +
                                      "COLLATE='utf32_general_ci';";

                await command.ExecuteNonQueryAsync();
            });
        }

        private Task<bool> CreateAccountIntenalAsync(string uniqueId, decimal balance)
        {
            return ExecuteMySqlContextAsync(async command =>
            {
                command.Parameters.Add("@uniqueId", MySqlDbType.VarChar).Value = uniqueId;
                command.Parameters.Add("@balance", MySqlDbType.Decimal).Value = balance;
                command.CommandText = $"INSERT IGNORE INTO `{m_TableName}` " +
                                      "(`UniqueId`, `Balance`) VALUES " +
                                      "(@uniqueId, @balance);";

                return await command.ExecuteNonQueryAsync() > 0;
            });
        }
    }
}