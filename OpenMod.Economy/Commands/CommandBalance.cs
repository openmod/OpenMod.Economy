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

namespace OpenMod.Economy.Commands;

[Command("balance", Priority = Priority.Normal)]
[CommandAlias("bal")]
[CommandDescription("Shows the player's balance")]
[CommandSyntax("[player]")]
[RegisterCommandPermission(OthersPerm, Description = "Permission to see the balance of other players")]
[UsedImplicitly]
public class CommandBalance(
    IEconomyProvider economyProvider,
    IServiceProvider serviceProvider,
    IStringLocalizer stringLocalizer)
    : Command(serviceProvider)
{
    // ReSharper disable once MemberCanBePrivate.Global
    public const string OthersPerm = "others";

    private bool m_IsSelf;
    private ICommandActor? m_TargetUser;

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

        await PrintAsync(stringLocalizer[msgKey.ToString(), new
        {
            Context.Actor,
            Balance = await economyProvider.GetBalanceAsync(m_TargetUser!.Id, m_TargetUser.Type),
            EconomyProvider = economyProvider,
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
            throw new UserFriendlyException(stringLocalizer["economy:fail:user_not_found",
                new { Input = await Context.Parameters.GetAsync<string>(0) }]);
        }
    }
}