#region

using OpenMod.Core.Eventing;

#endregion

namespace OpenMod.Economy.Events
{
    public class ChangeBalanceEvent : Event
    {
        public ChangeBalanceEvent(string ownerId, string ownerType, decimal balance, decimal amount)
        {
            OwnerId = ownerId;
            OwnerType = ownerType;
            Balance = balance;
            Amount = amount;
        }

        public string OwnerId { get; }
        public string OwnerType { get; }
        public decimal Balance { get; }
        public decimal Amount { get; }
    }
}