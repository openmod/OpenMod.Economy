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
    [Command("balance", Priority = Priority.Normal)]
    [CommandAlias("bal")]
    [CommandDescription("Shows the player's balance")]
    [CommandSyntax("[player]")]
    [RegisterCommandPermission(OthersPerm, Description = "Permission to see the balance of other players")]
    [UsedImplicitly]
    public class CommandBalance : Command
    {
        public const string OthersPerm = "others";

        private readonly IEconomyProvider m_EconomyProvider;
        private readonly IStringLocalizer m_StringLocalizer;

        public CommandBalance(IEconomyProvider economyProvider,
            IServiceProvider serviceProvider, IStringLocalizer stringLocalizer) : base(
            serviceProvider)
        {
            m_EconomyProvider = economyProvider;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            if (Context.Parameters.Length > 0 && await CheckPermissionAsync(OthersPerm) == PermissionGrantResult.Grant)
            {
                var targetUser = await Context.Parameters.GetAsync<IUser>(0);
                if (targetUser == null)
                    throw new UserFriendlyException(m_StringLocalizer["economy:fail:user_not_found",
                        new {Input = await Context.Parameters.GetAsync<string>(0)}]);

                if (!Context.Actor.Equals(targetUser))
                {
                    await PrintAsync(m_StringLocalizer["economy:success:show_balance_other", new
                    {
                        Context.Actor,
                        Balance = await m_EconomyProvider.GetBalanceAsync(targetUser.Id, targetUser.Type),
                        EconomyProvider = m_EconomyProvider,
                        Target = targetUser
                    }]);
                    return;
                }
            }

            await PrintAsync(m_StringLocalizer["economy:success:show_balance",
                new
                {
                    Context.Actor,
                    Balance = await m_EconomyProvider.GetBalanceAsync(Context.Actor.Id, Context.Actor.Type),
                    EconomyProvider = m_EconomyProvider
                }]);
        }
    }
}