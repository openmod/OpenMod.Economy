using System;
using OpenMod.Economy.API;
using YamlDotNet.Serialization;

namespace OpenMod.Economy.Models;

[Serializable]
public class DatabaseSettings
{
    [YamlMember(Alias = "Connection_String")]
    public string ConnectionString { get; set; } = "{WorkingDirectory}/file.db";

    [YamlMember(Alias = "Store_Type")] public StoreType DbType { get; set; } = StoreType.DataStore;

    [YamlMember(Alias = "Table_Name")] public string TableName { get; set; } = "Economy";
}