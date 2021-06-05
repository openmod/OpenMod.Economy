#region

using System;
using System.Text;
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

        private bool m_IsSelf;
        private ICommandActor m_TargetUser;

        public CommandBalance(IEconomyProvider economyProvider,
            IServiceProvider serviceProvider, IStringLocalizer stringLocalizer) : base(
            serviceProvider)
        {
            m_EconomyProvider = economyProvider;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            await GetTarget();
            await DisplayMsg();
        }

        private async Task DisplayMsg()
        {
            var msgKey = new StringBuilder("economy:success:show_balance");
            if (!m_IsSelf)
                msgKey.Append("_other");

            await PrintAsync(m_StringLocalizer[msgKey.ToString(), new
            {
                Context.Actor,
                Balance = await m_EconomyProvider.GetBalanceAsync(m_TargetUser.Id, m_TargetUser.Type),
                EconomyProvider = m_EconomyProvider,
                Target = m_TargetUser
            }]);
        }

        private async Task<bool> IsDenied(string permission)
        {
            return await CheckPermissionAsync(permission) != PermissionGrantResult.Grant;
        }

        private async Task GetTarget()
        {
            if (Context.Parameters.Length < 1 || await IsDenied(OthersPerm))
            {
                m_IsSelf = true;
                m_TargetUser = Context.Actor;
                return;
            }

            try
            {
                m_TargetUser = await Context.Parameters.GetAsync<IUser>(0);
                m_IsSelf = Context.Actor.Equals(m_TargetUser);
            }
            catch (CommandParameterParseException)
            {
                throw new UserFriendlyException(m_StringLocalizer["economy:fail:user_not_found",
                    new {Input = await Context.Parameters.GetAsync<string>(0)}]);
            }
        }
    }
}