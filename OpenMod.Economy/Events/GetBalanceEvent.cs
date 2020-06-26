#region

using OpenMod.Core.Eventing;

#endregion

namespace OpenMod.Economy.Events
{
    public class GetBalanceEvent : Event
    {
        public GetBalanceEvent(string userId, string userType, decimal balance)
        {
            UserId = userId;
            UserType = userType;
            Balance = balance;
        }

        public string UserId { get; }
        public string UserType { get; }
        public decimal Balance { get; }
    }
}