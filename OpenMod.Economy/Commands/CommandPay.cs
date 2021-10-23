#region

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Permissions;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Commands
{
    [Command("pay", Priority = Priority.Normal)]
    [CommandDescription("Pay to a user")]
    [CommandSyntax("<player> <amount> [reason]")]
    [RegisterCommandPermission(BankAccount,
        Description = "Any user with this permission pays without withdrawing money from the account")]
    [RegisterCommandPermission(Negative,
        Description = "Any user with this permission can withdraw money from other users")]
    [RegisterCommandPermission(PayToSelf, Description = "Permission to increase/decrease the own balance")]
    [UsedImplicitly]
    public class CommandPay : Command
    {
        public const string BankAccount = "bank";
        public const string Negative = "negative";
        public const string PayToSelf = "self";

        private readonly IEconomyProvider m_EconomyProvider;
        private readonly bool m_SetNegative;
        private readonly IStringLocalizer m_StringLocalizer;

        private decimal? m_ActorBalance;

        private decimal m_Amount;
        private bool m_IsAmountNegative;

        private bool m_IsSelf;

        private string m_Reason;
        private decimal m_TargetBalance;
        private ICommandActor m_TargetUser;

        public CommandPay(IConfiguration configuration, IEconomyProvider economyProvider,
            IServiceProvider serviceProvider, IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_EconomyProvider = economyProvider;
            m_StringLocalizer = stringLocalizer;

            m_SetNegative = configuration.GetSection("Economy:Set_Negative_Zero").Get<bool>();
        }

        protected override async Task OnExecuteAsync()
        {
            if (Context.Parameters.Length < 2)
                throw new CommandWrongUsageException(Context);

            await GetTarget();
            await GetAmount();
            GetReason();

            await UpdateBalanceAndDisplay();
        }

        #region Data

        private async Task GetAmount()
        {
            try
            {
                m_Amount = await Context.Parameters.GetAsync<decimal>(1);
            }
            catch (CommandParameterParseException)
            {
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount",
                    new {Amount = await Context.Parameters.GetAsync<string>(1)}]);
            }

            switch (m_Amount)
            {
                case > 0:
                    return;

                case < 0 when m_IsSelf || !await IsDenied(Negative):
                    m_IsAmountNegative = true;
                    return;

                default:
                    throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount",
                        new {Amount = m_Amount}]);
            }
        }

        private void GetReason()
        {
            m_Reason = Context.Parameters.Length > 2
                ? Context.Parameters.GetArgumentLine(2)
                : m_StringLocalizer["economy:default:payment_reason", new
                {
                    Context.Actor,
                    Amount = m_Amount,
                    EconomyProvider = m_EconomyProvider,
                    Target = m_Reason
                }];
        }

        private async Task GetTarget()
        {
            try
            {
                m_TargetUser = await Context.Parameters.GetAsync<IUser>(0);
                m_IsSelf = Context.Actor.Equals(m_TargetUser);

                if (m_IsSelf && await IsDenied(PayToSelf))
                    throw new UserFriendlyException(m_StringLocalizer["economy:fail:self_pay"]);
            }
            catch (CommandParameterParseException)
            {
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:user_not_found",
                    new {Input = await Context.Parameters.GetAsync<string>(0)}]);
            }
        }

        private async Task<bool> IsDenied(string permission)
        {
            return await CheckPermissionAsync(permission) != PermissionGrantResult.Grant;
        }

        #endregion

        #region Process

        private async Task<decimal> UpdateBalance(IPermissionActor actor, decimal amount)
        {
            try
            {
                return await m_EconomyProvider.UpdateBalanceAsync(actor.Id, actor.Type, amount, m_Reason);
            }
            catch (NotEnoughBalanceException ex)
            {
                if (!m_IsAmountNegative)
                    throw;

                if (m_SetNegative)
                {
                    await m_EconomyProvider.SetBalanceAsync(actor.Id, actor.Type, 0);
                    return 0;
                }

                if (m_IsSelf)
                    throw;

                throw new NotEnoughBalanceException(
                    m_StringLocalizer["economy:fail:not_enough_balance_negative",
                        new {Amount = amount, EconomyProvider = m_EconomyProvider, Target = actor}], ex.Balance!.Value);
            }
        }

        private async Task UpdateBalanceAndDisplay()
        {
            if (m_IsSelf)
            {
                await UpdateBalanceAndDisplaySelf();
                return;
            }

            if (m_IsAmountNegative)
            {
                m_TargetBalance = await UpdateBalance(m_TargetUser, m_Amount);
                m_ActorBalance = await UpdateBalance(Context.Actor, -m_Amount);
            }
            else
            {
                if (await IsDenied(BankAccount))
                    m_ActorBalance = await UpdateBalance(Context.Actor, -m_Amount);
                m_TargetBalance = await UpdateBalance(m_TargetUser, m_Amount);
            }

            await PrintAsync(m_StringLocalizer[
                m_ActorBalance.HasValue ? "economy:success:pay_player" : "economy:success:pay_bank", new
                {
                    Context.Actor,
                    Amount = m_Amount,
                    Balance = m_ActorBalance ?? m_TargetBalance,
                    EconomyProvider = m_EconomyProvider,
                    Target = m_TargetUser
                }]);
            await m_TargetUser.PrintMessageAsync(m_StringLocalizer[
                m_IsAmountNegative ? "economy:success:payed_negative" : "economy:success:payed", new
                {
                    Context.Actor,
                    Amount = Math.Abs(m_Amount),
                    Balance = m_TargetBalance,
                    EconomyProvider = m_EconomyProvider,
                    Target = m_TargetUser
                }]);
        }

        private async Task UpdateBalanceAndDisplaySelf()
        {
            m_ActorBalance = await UpdateBalance(Context.Actor, m_Amount);
            await PrintAsync(m_StringLocalizer["economy:success:pay_self", new
            {
                Context.Actor,
                Amount = m_Amount,
                Balance = m_ActorBalance,
                EconomyProvider = m_EconomyProvider
            }]);
        }

        #endregion
    }
}
