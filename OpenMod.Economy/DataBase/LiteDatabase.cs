using System;
using System.Threading.Tasks;
using LiteDB;
using OpenMod.Economy.Models;

namespace OpenMod.Economy.DataBase;

internal sealed class LiteDatabase(IServiceProvider serviceProvider) : Database(serviceProvider)
{
    public override Task<bool> CheckSchemasAsync()
    {
        return RunQueryNonEnqueueAsync(connection => connection.CollectionExists(TableName), true);
    }

    public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
    {
        var uniqueId = $"{ownerType}_{ownerId}";
        return RunQueryAsync(accounts =>
        {
            var account = accounts.FindById(uniqueId);
            return account?.Balance ?? DefaultBalance;
        }, true);
    }

    public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string? reason)
    {
        var uniqueId = $"{ownerType}_{ownerId}";
        return RunQueryAsync(accounts =>
        {
            var account = accounts.FindById(uniqueId) ?? new AccountBase
            {
                UniqueId = uniqueId,
                Balance = DefaultBalance
            };

            var newBalance = account.Balance + amount;
            if (newBalance < 0)
                throw ThrowNotEnoughtBalance(amount, account.Balance);

            account.Balance = newBalance;
            accounts.Upsert(account);
            return newBalance;
        });
    }

    public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
    {
        var uniqueId = $"{ownerType}_{ownerId}";
        return RunQueryAsync(accounts =>
        {
            var account = accounts.FindById(uniqueId) ?? new AccountBase { UniqueId = uniqueId };

            account.Balance = balance;
            accounts.Upsert(account);
        });
    }

    private Task<TReturn> RunQueryNonEnqueueAsync<TReturn>(Func<ILiteDatabase, TReturn> action, bool readOnly = false)
    {
        return RunConnectionAsync(action, readOnly);
    }

    private Task<TReturn> RunQueryAsync<TReturn>(Func<ILiteCollection<AccountBase>, TReturn> action,
        bool readOnly = false)
    {
        return Enqueue(() => RunConnectionAsync(database => RunCommandAsync(database, action), readOnly));
    }

    private Task RunQueryAsync(Action<ILiteCollection<AccountBase>> action, bool readOnly = false)
    {
        return Enqueue(() => RunConnectionAsync(database => RunCommandAsync(database, action), readOnly));
    }

    private Task<TReturn> RunConnectionAsync<TReturn>(Func<ILiteDatabase, TReturn> action, bool readOnly = false)
    {
        var connection = new ConnectionString(ConnectionString) { ReadOnly = readOnly };
        using var database = new LiteDB.LiteDatabase(connection);
        var result = action(database);
        return Task.FromResult(result);
    }

    private Task RunConnectionAsync(Action<ILiteDatabase> action, bool readOnly = false)
    {
        var connection = new ConnectionString(ConnectionString) { ReadOnly = readOnly };
        using var database = new LiteDB.LiteDatabase(connection);
        action(database);
        return Task.CompletedTask;
    }

    private TReturn RunCommandAsync<TReturn>(ILiteDatabase database, Func<ILiteCollection<AccountBase>, TReturn> action)
    {
        var collection = database.GetCollection<AccountBase>(TableName);
        return action(collection);
    }

    private void RunCommandAsync(ILiteDatabase database, Action<ILiteCollection<AccountBase>> action)
    {
        var collection = database.GetCollection<AccountBase>(TableName);
        action(collection);
    }
}