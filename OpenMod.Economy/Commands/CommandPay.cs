#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Users;

#endregion

namespace OpenMod.Economy.Commands
{
    [Command("pay", Priority = Priority.Normal)]
    [CommandDescription("Pay to a user")]
    [CommandSyntax("<player> <amount>")]
    public class CommandPay : Command
    {
        private readonly Economy m_Plugin;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IUserManager m_UserManager;


        public CommandPay(Economy plugin, IStringLocalizer stringLocalizer, IUserManager userManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Plugin = plugin;
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
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:invalid_amount", amount]);

            var target = await Context.Parameters.GetAsync<string>(0);
            var targetPlayer =
                await m_UserManager.FindUserAsync(KnownActorTypes.Player, target, UserSearchMode.NameOrId);

            if (targetPlayer == null)
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:user_not_found", target]);

            if (targetPlayer.Id.Equals(Context.Actor.Id))
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:self_pay"]);

            var contextActorBalance = (decimal?) null;
            if (!isConsole) contextActorBalance = await m_Plugin.DataBase.UpdateBalanceAsync(Context.Actor.Id, Context.Actor.Type, -amount);

            var targetBalance = await m_Plugin.DataBase.UpdateBalanceAsync(targetPlayer.Id, targetPlayer.Type, amount);

            await PrintAsync(contextActorBalance.HasValue
                ? m_StringLocalizer["economy:success:pay_player", targetPlayer.DisplayName, amount,
                    contextActorBalance.Value]
                : m_StringLocalizer["economy:success:pay_console", targetPlayer.DisplayName, amount, targetBalance]);
            await targetPlayer.PrintMessageAsync(m_StringLocalizer[
                isNegative ? "economy:success:payed_negative" : "economy:success:payed", Context.Actor.DisplayName,
                amount,
                targetBalance]);
        }
    }
}