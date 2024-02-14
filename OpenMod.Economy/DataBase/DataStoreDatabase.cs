using System;
using System.Threading.Tasks;
using OpenMod.API.Persistence;
using OpenMod.Economy.Models;
#if NETSTANDARD2_1_OR_GREATER
using System.Collections.Generic;
#endif

namespace OpenMod.Economy.DataBase;

internal sealed class DataStoreDatabase : Database
{
    private readonly IDataStore m_DataStore;

    public DataStoreDatabase(IDataStore dataStore, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        m_DataStore = dataStore;
    }

    public override Task<bool> CheckSchemasAsync()
    {
        return Enqueue(async () =>
        {
            if (await m_DataStore.ExistsAsync(TableName))
                return true;

            var data = new AccountsCollection();
            await m_DataStore.SaveAsync(TableName, data);
            return false;
        });
    }

    public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
    {
        var uniqueId = $"{ownerType}_{ownerId}";
#if NETSTANDARD2_1_OR_GREATER
        return RunQueryAsync(data => data.Accounts.GetValueOrDefault(uniqueId, DefaultBalance), true);
#else
        return RunQueryAsync(data => data.Accounts.TryGetValue(uniqueId, out var balance) ? balance : DefaultBalance, true);
#endif
    }

    public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string? _)
    {
        var uniqueId = $"{ownerType}_{ownerId}";
        return RunQueryAsync(data =>
        {
#if NETSTANDARD2_1_OR_GREATER
            var balance = data.Accounts.GetValueOrDefault(uniqueId, DefaultBalance);
#else
            if (!data.Accounts.TryGetValue(uniqueId, out var balance)) balance = DefaultBalance;
#endif

            var newBalance = balance + amount;
            if (newBalance < 0)
                throw ThrowNotEnoughtBalance(amount, balance);

            data.Accounts[uniqueId] = newBalance;
            return newBalance;
        }, false);
    }

    public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
    {
        var uniqueId = $"{ownerType}_{ownerId}";
        return RunQueryAsync(data => data.Accounts[uniqueId] = balance, false);
    }

    private Task<TReturn> RunQueryAsync<TReturn>(Func<AccountsCollection, TReturn> action, bool readOnly)
    {
        return Enqueue(async () =>
        {
            var data = await m_DataStore.LoadAsync<AccountsCollection>(TableName) ?? new AccountsCollection();
            var result = action(data);

            if (!readOnly)
                await m_DataStore.SaveAsync(TableName, data);
            return result;
        });
    }
}