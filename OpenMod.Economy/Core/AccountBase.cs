#region

using System;
using LiteDB;

#endregion

namespace OpenMod.Economy.Core
{
    public class AccountBase
    {
        [BsonIgnore] private string m_OwnerId;

        [BsonIgnore] private string m_OwnerType;

        [BsonId]
        public string UniqueId
        {
            get => $"{m_OwnerType}_{m_OwnerId}";
            set
            {
                var values = value.Split('_');
                if (values.Length != 2)
                    throw new Exception($"Invalid UniqueId: {UniqueId}");

                m_OwnerType = values[0].ToLower();
                m_OwnerId = values[1].ToLower();
            }
        }

        public decimal Balance { get; set; }
    }
}