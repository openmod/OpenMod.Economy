#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Commands
{
    [Command("pay", Priority = Priority.Normal)]
    [CommandDescription("Pay to a user")]
    [CommandSyntax("<player> <amount>")]
    public class CommandPay : Command
    {
        private readonly IEconomyProvider m_EconomyProvider;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IUserManager m_UserManager;

        public CommandPay(IEconomyProvider economyProvider, IServiceProvider serviceProvider,
            IStringLocalizer stringLocalizer, IUserManager userManager) : base(serviceProvider)
        {
            m_EconomyProvider = economyProvider;
            m_StringLocalizer = stringLocalizer;
            m_UserManager = userManager;
        }


        protected override async Task OnExecuteAsync()
        {
            if (Context.Parameters.Length < 2) throw new CommandWrongUsageException(Context);

            var amount = await Context.Parameters.GetAsync<decimal>(1);
            var isConsole = Context.Actor.Type == KnownActorTypes.Console;
            var isNegative = amount < 0;

            if (isNegative && !isConsole || amount == 0)
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount",
                    new {Amount = amount}]);

            var target = await Context.Parameters.GetAsync<string>(0);
            var targetPlayer =
                await m_UserManager.FindUserAsync(KnownActorTypes.Player, target, UserSearchMode.NameOrId);

            if (targetPlayer == null)
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:user_not_found", new {target}]);

            if (targetPlayer.Id.Equals(Context.Actor.Id))
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:self_pay"]);

            var contextActorBalance = (decimal?) null;
            if (!isConsole)
                contextActorBalance =
                    await m_EconomyProvider.UpdateBalanceAsync(Context.Actor.Id, Context.Actor.Type, -amount);

            var targetBalance = amount;
            if (Context.Parameters.Length == 3) //todo Need to be removed, just to test
                await m_EconomyProvider.SetBalanceAsync(targetPlayer.Id, targetPlayer.Type, amount);
            else
                targetBalance = await m_EconomyProvider.UpdateBalanceAsync(targetPlayer.Id, targetPlayer.Type, amount);

            var printToCaller = m_StringLocalizer[
                contextActorBalance.HasValue ? "economy:success:pay_player" : "economy:success:pay_console",
                new
                {
                    Amount = amount, Balance = contextActorBalance ?? targetBalance, m_EconomyProvider.CurrencyName,
                    m_EconomyProvider.CurrencySymbol, targetPlayer.DisplayName
                }];
            await PrintAsync(printToCaller);

            amount = Math.Abs(amount);
            var printToTarget = m_StringLocalizer[
                isNegative ? "economy:success:payed_negative" : "economy:success:payed",
                new
                {
                    Amount = amount, Balance = targetBalance, m_EconomyProvider.CurrencyName,
                    m_EconomyProvider.CurrencySymbol, Context.Actor.DisplayName
                }];
            await targetPlayer.PrintMessageAsync(printToTarget);
        }
    }
}