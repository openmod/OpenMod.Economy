#region

using OpenMod.Core.Eventing;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.Events
{
    public class ChangeBalanceEvent : Event
    {
        public ChangeBalanceEvent(IAccountId accountId, decimal balance, decimal amount)
        {
            AccountId = accountId;
            Balance = balance;
            Amount = amount;
        }

        public IAccountId AccountId { get; }
        public decimal Balance { get; }
        public decimal Amount { get; }
    }
}