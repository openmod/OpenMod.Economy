#region

using OpenMod.Core.Eventing;

#endregion

namespace OpenMod.Economy.Events
{
    public class SetAccountEvent : Event
    {
        public SetAccountEvent(string ownerId, string ownerType, decimal balance)
        {
            OwnerId = ownerId;
            OwnerType = ownerType;
            Balance = balance;
        }

        public string OwnerId { get; }
        public string OwnerType { get; }

        public decimal Balance { get; }
    }
}