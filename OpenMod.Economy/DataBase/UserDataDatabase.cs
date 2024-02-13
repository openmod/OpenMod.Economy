using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Users;

namespace OpenMod.Economy.DataBase;

internal sealed class UserDataDatabase(IServiceProvider serviceProvider) : Database(serviceProvider)
{
    private readonly IUserDataStore m_UserDataStore = serviceProvider.GetRequiredService<IUserDataStore>();

    public override Task<bool> CheckSchemasAsync()
    {
        return Task.FromResult(true);
    }

    public override Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
    {
        return Enqueue(async () =>
            await m_UserDataStore.GetUserDataAsync<decimal?>(ownerId, ownerType, TableName) ?? DefaultBalance);
    }

    public override Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string? _)
    {
        return Enqueue(async () =>
        {
            var balance = await m_UserDataStore.GetUserDataAsync<decimal?>(ownerId, ownerType, TableName) ??
                          DefaultBalance;

            var newBalance = balance + amount;
            if (newBalance < 0)
                throw ThrowNotEnoughtBalance(amount, balance);

            await m_UserDataStore.SetUserDataAsync(ownerId, ownerType, TableName, newBalance);
            return newBalance;
        });
    }

    public override Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
    {
        return Enqueue(() => m_UserDataStore.SetUserDataAsync(ownerId, ownerType, TableName, balance));
    }
}