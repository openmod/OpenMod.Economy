using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.Economy.API;
using OpenMod.Economy.Models;
using OpenMod.Extensions.Economy.Abstractions;

namespace OpenMod.Economy.DataBase;

public abstract class Database : IDatabase, IDisposable
{
    private readonly DatabaseSettings m_DatabaseSettings;


    private readonly IEconomyDispatcher m_EconomyDispatcher;
    private readonly EconomySettings m_EconomySettings;
    private readonly IStringLocalizer m_StringLocalizer;
    private CancellationTokenSource? m_CancellationTokenSource = new();

    protected Database(IServiceProvider serviceProvider)
    {
        m_DatabaseSettings = serviceProvider.GetRequiredService<DatabaseSettings>();
        m_EconomySettings = serviceProvider.GetRequiredService<EconomySettings>();
        m_EconomyDispatcher = serviceProvider.GetRequiredService<IEconomyDispatcher>();
        m_StringLocalizer = serviceProvider.GetRequiredService<IStringLocalizer>();
    }

    protected string ConnectionString => m_DatabaseSettings.ConnectionString;
    protected string TableName => m_DatabaseSettings.TableName;

    // ReSharper disable once MemberCanBeProtected.Global
    public decimal DefaultBalance => m_EconomySettings.DefaultBalance;

    /// <inheritdoc />
    public string CurrencyName => m_EconomySettings.CurrencyName;

    /// <inheritdoc />
    public string CurrencySymbol => m_EconomySettings.CurrencySymbol;

    public abstract Task<bool> CheckSchemasAsync();

    /// <inheritdoc />
    public abstract Task<decimal> GetBalanceAsync(string ownerId, string ownerType);

    /// <inheritdoc />
    public abstract Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal changeAmount,
        string? reason);

    /// <inheritdoc />
    public abstract Task SetBalanceAsync(string ownerId, string ownerType, decimal balance);

    public virtual void Dispose()
    {
        m_CancellationTokenSource?.Cancel();
        m_CancellationTokenSource?.Dispose();
        m_CancellationTokenSource = null;
    }


    protected Task<TReturn> Enqueue<TReturn>(Func<Task<TReturn>> task)
    {
        return m_EconomyDispatcher.EnqueueV2(task);
    }

    protected Task Enqueue(Func<Task> task)
    {
        return m_EconomyDispatcher.EnqueueV2(task);
    }

    protected CancellationToken GetCancellationToken()
    {
        if (m_CancellationTokenSource is null)
            throw new ObjectDisposedException(GetType().Name, "Database have already been disposed.");

        return m_CancellationTokenSource.Token;
    }

    protected Exception ThrowNotEnoughtBalance(decimal amount, decimal balance)
    {
        throw new NotEnoughBalanceException(
            m_StringLocalizer["economy:fail:not_enough_balance",
                new { Amount = -amount, Balance = balance, EconomyProvider = (IEconomyProvider)this }], balance);
    }
}