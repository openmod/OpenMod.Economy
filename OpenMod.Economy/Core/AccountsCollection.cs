#region

using System.Collections.Generic;
using OpenMod.Economy.API;

#endregion

namespace OpenMod.Economy.Core
{
    public class AccountsCollection
    {
        public Dictionary<IAccountId, decimal> Accounts = new Dictionary<IAccountId, decimal>();
    }
}