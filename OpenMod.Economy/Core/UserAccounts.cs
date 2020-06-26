#region

using System;
using System.Collections.Generic;

#endregion

namespace OpenMod.Economy.Core
{
    public class UserAccounts
    {
        public Dictionary<string, decimal> Accounts = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }
}