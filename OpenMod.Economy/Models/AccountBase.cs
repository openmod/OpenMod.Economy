using System;
using LiteDB;

namespace OpenMod.Economy.Models;

public class AccountBase
{
    [BsonIgnore] private string? m_OwnerId;

    [BsonIgnore] private string? m_OwnerType;

    [BsonId]
    public string UniqueId
    {
        get => $"{m_OwnerType}_{m_OwnerId}";
        set
        {
            var index = value.IndexOf('_');
            if (index == -1)
                throw new Exception($"Invalid UniqueId: {UniqueId}");

            var type = value[..index];
            if (string.IsNullOrEmpty(type))
                throw new Exception($"Invalid UniqueId: {UniqueId}");

            var id = value[(index + 1)..];
            if (string.IsNullOrEmpty(id))
                throw new Exception($"Invalid UniqueId: {UniqueId}");

            m_OwnerType = type;
            m_OwnerId = id;
        }
    }

    public decimal Balance { get; set; }
}