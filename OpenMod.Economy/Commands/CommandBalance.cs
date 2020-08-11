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
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;

#endregion

namespace OpenMod.Economy.Commands
{
    [Command("balance", Priority = Priority.Normal)]
    [CommandDescription("Shows the player's balance")]
    [CommandSyntax("[player]")]
    [RegisterCommandPermission(OthersPerm, Description = "Permission to see the balance of other players")]
    [UsedImplicitly]
    public class CommandBalance : Command
    {
        public const string OthersPerm = "others";

        private readonly IEconomyProvider m_EconomyProvider;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IUserManager m_UserManager;

        public CommandBalance(IEconomyProvider economyProvider,
            IServiceProvider serviceProvider, IStringLocalizer stringLocalizer, IUserManager userManager) : base(
            serviceProvider)
        {
            m_EconomyProvider = economyProvider;
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
                var otherPermission = await CheckPermissionAsync(OthersPerm) == PermissionGrantResult.Grant;
                var target = await Context.Parameters.GetAsync<string>(0);
                targetData =
                    await m_UserManager.FindUserAsync(KnownActorTypes.Player, target, UserSearchMode.FindByNameOrId);

                if (otherPermission)
                {
                    if (targetData == null)
                        throw new UserFriendlyException(m_StringLocalizer["economy:fail:user_not_found", new {target}]);

                    if (!Context.Actor.Id.Equals(targetData.Id, StringComparison.OrdinalIgnoreCase))
                        other = true;
                }
            }

            if (!other)
                targetData =
                    await m_UserManager.FindUserAsync(Context.Actor.Type, Context.Actor.Id, UserSearchMode.FindById);

            var balance = await m_EconomyProvider.GetBalanceAsync(targetData.Id, targetData.Type);
            var message = other
                ? m_StringLocalizer["economy:success:show_balance_other",
                    new {Balance = balance, m_EconomyProvider.CurrencySymbol, targetData.DisplayName}]
                : m_StringLocalizer["economy:success:show_balance",
                    new {Balance = balance, m_EconomyProvider.CurrencySymbol}];

            await PrintAsync(message);
        }
    }
}