using System;
using YamlDotNet.Serialization;

namespace OpenMod.Economy.Models;

[Serializable]
public class EconomySettings
{
    public string CurrencyName { get; set; } = "Cash";
    public string CurrencySymbol { get; set; } = "$";

    [YamlMember(Alias = "Default_Balance")]
    public decimal DefaultBalance { get; set; } = 300;

    [YamlMember(Alias = "Set_Negative_Zero")]
    public bool NegativeToZero { get; set; } = true;
}