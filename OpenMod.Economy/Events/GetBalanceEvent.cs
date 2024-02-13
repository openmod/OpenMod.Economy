using OpenMod.Core.Eventing;

namespace OpenMod.Economy.Events;

public class GetBalanceEvent(string ownerId, string ownerType, decimal balance) : Event
{
    // ReSharper disable UnusedMember.Global
    public string OwnerId { get; } = ownerId;
    public string OwnerType { get; } = ownerType;

    public decimal Balance { get; } = balance;
    // ReSharper restore UnusedMember.Global
}