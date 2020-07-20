#region

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Commands
{
    [Command("balance", Priority = Priority.Normal)]
    [CommandDescription("Get the balance of a player")]
    [CommandSyntax("[player]")]
    public class CommandBalance : Command
    {
        private readonly IEconomyProvider m_EconomyProvider;
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IUserManager m_UserManager;

        public CommandBalance(IEconomyProvider economyProvider, IPermissionChecker permissionChecker,
            IServiceProvider serviceProvider, IStringLocalizer stringLocalizer, IUserManager userManager) : base(
            serviceProvider)
        {
            m_EconomyProvider = economyProvider;
            m_PermissionChecker = permissionChecker;
            m_StringLocalizer = stringLocalizer;
            m_UserManager = userManager;
        }

        protected override async Task OnExecuteAsync()
        {
            if (Context.Actor.Type == KnownActorTypes.Console && Context.Parameters.Length == 0)
                throw new CommandWrongUsageException(Context);

            var other = false;
            var targetData = (IUser) null; //i know, blame CS0165

            if (Context.Parameters.Length > 0)
            {
                var otherPermission = await m_PermissionChecker.CheckPermissionAsync(Context.Actor, "balance.other") ==
                                      PermissionGrantResult.Grant;
                var target = await Context.Parameters.GetAsync<string>(0);
                targetData = await m_UserManager.FindUserAsync(KnownActorTypes.Player, target, UserSearchMode.NameOrId);

                if (otherPermission)
                {
                    if (targetData == null)
                        throw new UserFriendlyException(m_StringLocalizer["economy:fail:user_not_found", target]);

                    if (!Context.Actor.Id.Equals(targetData.Id, StringComparison.OrdinalIgnoreCase))
                        other = true;
                }
            }

            if (!other)
                targetData = await m_UserManager.FindUserAsync(Context.Actor.Type, Context.Actor.Id, UserSearchMode.Id);

            var balance = await m_EconomyProvider.GetBalanceAsync(targetData.Id, targetData.Type);
            var message = other
                ? m_StringLocalizer["economy:success:show_balance_other", balance, targetData.DisplayName]
                : m_StringLocalizer["economy:success:show_balance", balance];
            await PrintAsync(message);
        }
    }
}