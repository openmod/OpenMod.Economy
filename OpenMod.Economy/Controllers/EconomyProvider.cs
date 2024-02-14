using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API;
using OpenMod.API.Commands;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.Economy.API;
using OpenMod.Economy.Events;
using OpenMod.Economy.Models;
using OpenMod.Extensions.Economy.Abstractions;

namespace OpenMod.Economy.Controllers;

[ServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
[UsedImplicitly]
public sealed class EconomyProvider : IEconomyProvider
{
    private readonly EconomySettings m_EconomySettings;
    private readonly IEventBus m_EventBus;
    private readonly IOpenModComponent m_OpenModComponent;
    private readonly IServiceProvider m_ServiceProvider;
    private readonly IStringLocalizer m_StringLocalizer;

    public EconomyProvider(EconomySettings economySettings, IEventBus eventBus, IOpenModComponent openModComponent,
        IServiceProvider serviceProvider, IStringLocalizer stringLocalizer)
    {
        m_EconomySettings = economySettings;
        m_EventBus = eventBus;
        m_OpenModComponent = openModComponent;
        m_ServiceProvider = serviceProvider;
        m_StringLocalizer = stringLocalizer;
    }

    private IDatabase Database => m_ServiceProvider.GetRequiredService<IDatabase>();
    public string CurrencyName => m_EconomySettings.CurrencyName;
    public string CurrencySymbol => m_EconomySettings.CurrencySymbol;

    public async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
    {
        var balance = await Database.GetBalanceAsync(ownerId, ownerType);

        var getBalanceEvent = new GetBalanceEvent(ownerId, ownerType, balance);
        await m_EventBus.EmitAsync(m_OpenModComponent, this, getBalanceEvent);

        return balance;
    }

    public async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal amount, string? reason)
    {
        if (amount == 0)
            throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount", new { Amount = amount }]);

        var database = Database;
        var oldBalance = await database.GetBalanceAsync(ownerId, ownerType);
        var balance = await database.UpdateBalanceAsync(ownerId, ownerType, amount, reason);

        var getBalanceEvent = new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, balance, reason);
        await m_EventBus.EmitAsync(m_OpenModComponent, this, getBalanceEvent);

        return balance;
    }

    public async Task SetBalanceAsync(string ownerId, string ownerType, decimal newBalance)
    {
        var database = Database;
        var oldBalance = await database.GetBalanceAsync(ownerId, ownerType);
        await database.SetBalanceAsync(ownerId, ownerType, newBalance);

        var getBalanceEvent =
            new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, newBalance, "Set Balance Requested");
        await m_EventBus.EmitAsync(m_OpenModComponent, this, getBalanceEvent);
    }
}