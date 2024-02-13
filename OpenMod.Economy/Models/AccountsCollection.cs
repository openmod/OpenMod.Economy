using System;
using System.Collections.Generic;

namespace OpenMod.Economy.Models;

[Serializable]
public class AccountsCollection
{
    public Dictionary<string, decimal> Accounts { get; set; } = new();
}