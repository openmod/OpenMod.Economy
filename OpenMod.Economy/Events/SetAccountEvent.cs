#region

using OpenMod.Core.Eventing;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.Events
{
    public class SetAccountEvent : Event
    {
        public SetAccountEvent(IAccountId accountId, decimal balance)
        {
            AccountId = accountId;
            Balance = balance;
        }

        public IAccountId AccountId { get; }

        public decimal Balance { get; }
    }
}