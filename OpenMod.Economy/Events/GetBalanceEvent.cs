using OpenMod.Core.Eventing;

namespace OpenMod.Economy.Events;

public class GetBalanceEvent : Event
{
    public GetBalanceEvent(string ownerId, string ownerType, decimal balance)
    {
        OwnerId = ownerId;
        OwnerType = ownerType;
        Balance = balance;
    }

    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public string OwnerId { get; }
    public string OwnerType { get; }

    public decimal Balance { get; }
    // ReSharper restore UnusedAutoPropertyAccessor.Global
}