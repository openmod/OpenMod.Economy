#region

using OpenMod.Core.Eventing;

#endregion

namespace OpenMod.Economy.Events
{
    public class ChangeBalanceEvent : Event
    {
        public ChangeBalanceEvent(string userId, string userType, decimal balance, decimal amount)
        {
            UserId = userId;
            UserType = userType;
            Balance = balance;
            Amount = amount;
        }

        public string UserId { get; }
        public string UserType { get; }
        public decimal Balance { get; }
        public decimal Amount { get; }
    }
}