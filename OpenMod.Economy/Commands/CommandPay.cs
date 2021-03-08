#region

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
        private readonly IStringLocalizer m_StringLocalizer;

        public CommandPay(IEconomyProvider economyProvider, IServiceProvider serviceProvider,
            IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_EconomyProvider = economyProvider;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            if (Context.Parameters.Length < 2) throw new CommandWrongUsageException(Context);

            var amount = await Context.Parameters.GetAsync<decimal>(1);
            IUser targetUser;
            try
            {
                targetUser = await Context.Parameters.GetAsync<IUser>(0);
            }
            catch (CommandParameterParseException)
            {
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:user_not_found",
                    new {Input = await Context.Parameters.GetAsync<string>(0)}]);
            }

            var self = Context.Actor.Equals(targetUser);
            if (self && await CheckPermissionAsync(PayToSelf) != PermissionGrantResult.Grant)
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:self_pay"]);

            if (amount == 0 || !self && amount < 0 &&
                await CheckPermissionAsync(Negative) != PermissionGrantResult.Grant)
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount",
                    new {Amount = amount}]);

            string reason;
            if (Context.Parameters.Length < 3)
                reason = m_StringLocalizer["economy:default:payment_reason", new
                {
                    Context.Actor,
                    Amount = amount,
                    EconomyProvider = m_EconomyProvider,
                    Target = targetUser
                }];
            else
                reason = Context.Parameters.GetArgumentLine(2);

            var contextActorBalance = (decimal?) null;
            if (!self && await CheckPermissionAsync(BankAccount) != PermissionGrantResult.Grant)
                contextActorBalance =
                    await m_EconomyProvider.UpdateBalanceAsync(Context.Actor.Id, Context.Actor.Type, -amount, reason);

            var targetBalance =
                await m_EconomyProvider.UpdateBalanceAsync(targetUser.Id, targetUser.Type, amount, reason);
            if (self)
            {
                var printSelf = m_StringLocalizer["economy:success:pay_self", new
                {
                    Context.Actor,
                    Amount = amount,
                    Balance = targetBalance,
                    EconomyProvider = m_EconomyProvider,
                    Target = targetUser
                }];

                await PrintAsync(printSelf);
                return;
            }

            var printToCaller = m_StringLocalizer[
                contextActorBalance.HasValue ? "economy:success:pay_player" : "economy:success:pay_bank", new
                {
                    Context.Actor,
                    Amount = amount,
                    Balance = contextActorBalance ?? targetBalance,
                    EconomyProvider = m_EconomyProvider,
                    Target = targetUser
                }];
            await PrintAsync(printToCaller);

            var printToTarget = m_StringLocalizer[
                amount < 0 ? "economy:success:payed_negative" : "economy:success:payed", new
                {
                    Context.Actor,
                    Amount = Math.Abs(amount),
                    Balance = contextActorBalance ?? targetBalance,
                    EconomyProvider = m_EconomyProvider,
                    Target = targetUser
                }];
            await targetUser.PrintMessageAsync(printToTarget);
        }
    }
}